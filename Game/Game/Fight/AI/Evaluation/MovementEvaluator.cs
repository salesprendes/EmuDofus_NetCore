using Game.Fight.AI.Core;
using Game.Fight;
using Game.Entity;
using Game.Map;
using Game.Spell;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    public sealed class MovementEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (!CanMove(context))
                yield break;

            if (IsMeleeOnly(context))
            {
                if (IsSelfLowHP(context, 0.25))
                {
                    var fleeCell = GetBestCellAwayFromEnemies(context);
                    if (fleeCell.HasValue)
                        yield return AIDecision.Move(fleeCell.Value, 120, AIDecisionPriority.Low, "Huida cuerpo a cuerpo - vida baja");
                    yield break;
                }

                if (HasSingleRangedEnemy(context))
                {
                    var forcedCell = GetBestCellForMaxApproach(context);
                    if (forcedCell.HasValue)
                    {
                        yield return AIDecision.Move(forcedCell.Value, 120, AIDecisionPriority.Low, "Forzar acercamiento contra enemigo a distancia");
                        yield break;
                    }
                }

                var meleeAttackCell = GetBestCellForAggressiveApproach(context);
                if (meleeAttackCell.HasValue)
                {
                    yield return AIDecision.Move(meleeAttackCell.Value, 100, AIDecisionPriority.Low, "Acercamiento cuerpo a cuerpo para atacar");
                    yield break;
                }

                var meleeNearCell = GetBestCellNearEnemy(context);
                if (meleeNearCell.HasValue)
                    yield return AIDecision.Move(meleeNearCell.Value, 60, AIDecisionPriority.Low, "Acercarse para cuerpo a cuerpo");
            }
            else
            {
                // Sin lanzamientos útiles este turno (PA agotados o hechizos bloqueados): un
                // atacante a distancia se retira fuera del alcance enemigo con los PM sobrantes
                // (kiting) en vez de quedarse quieto o acercarse sin motivo al cuerpo a cuerpo.
                if (GetCastableDamageSpells(context).Count == 0)
                {
                    var kiteCell = GetBestKitingCell(context);
                    if (kiteCell.HasValue)
                        yield return AIDecision.Move(kiteCell.Value, 90, AIDecisionPriority.Low, "Retirada tactica (sin PA utiles)");
                    yield break;
                }

                var attackCell = GetBestCellForDistanceAttack(context);
                if (attackCell.HasValue)
                    yield return AIDecision.Move(attackCell.Value, 100, AIDecisionPriority.Low, "Moverse para lanzar hechizo");

                var nearCell = GetBestCellNearEnemy(context);
                if (nearCell.HasValue)
                    yield return AIDecision.Move(nearCell.Value, 60, AIDecisionPriority.Low, "Acercarse al objetivo");
            }
        }

        /// <summary>
        /// Mejor celda de retirada: alcanzable, con amenaza estrictamente menor que la actual
        /// (el mapa de riesgo ya modela alcance y línea de visión de cada enemigo), desempatando
        /// por distancia al enemigo más cercano. Null si ya estamos a salvo o no hay mejora.
        /// </summary>
        public int? GetBestKitingCell(AIContext context)
        {
            if (!CanMove(context) || context?.Enemies == null)
                return null;

            var currentRisk = RiskEvaluator.ScoreCellRisk(context, context.CurrentCellId, false);
            if (currentRisk <= 0)
                return null;

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId)
                    continue;

                var risk = RiskEvaluator.ScoreCellRisk(context, cellId, false);
                if (risk >= currentRisk)
                    continue;

                var nearest = context.Enemies.Where(e => e?.Cell != null && !e.IsFighterDead)
                    .Select(e => context.TurnCache.Cells.GetDistance(cellId, e.Cell.Id))
                    .DefaultIfEmpty(0).Min();

                var score = (currentRisk - risk) * 3 + nearest * 5;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        private static bool IsMeleeOnly(AIContext context)
        {
            var spells = context?.SpellBook?.DamageSpells;
            if (spells == null || spells.Count == 0) return false;
            return spells.All(spell => spell != null && GetEffectiveMaxRange(context, spell) <= 1);
        }

        private static int GetEffectiveMaxRange(AIContext context, SpellLevel spell)
        {
            if (spell == null)
                return 0;

            var maxPo = spell.AllowPOBoost && spell.MaxPO != 0 && context?.Fighter?.Statistics != null ? spell.MaxPO + context.Fighter.Statistics.GetTotal(EffectEnum.STAT_MAS_ALCANCE) : spell.MaxPO;

            return System.Math.Max(spell.MinPO, maxPo);
        }

        private static bool IsSelfLowHP(AIContext context, double threshold)
        {
            var f = context?.Fighter;
            return f != null && f.MaxLife > 0 && (double)f.Life / f.MaxLife < threshold;
        }

        public int? GetBestCellNearEnemy(AIContext context)
        {
            if (!CanMove(context))
                return null;

            var target = TargetEvaluator.GetNearestEnemy(context);
            if (context?.Fighter?.Cell == null || target?.Cell == null || context.CurrentMP <= 0)
                return null;

            var approach = context.TurnCache.Cells.GetApproachDistances(target.Cell.Id);
            if (!approach.TryGetValue(context.CurrentCellId, out var currentDistance))
                return GetBestCellNearEnemyByGoalDistance(context, target);

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId)
                    continue;

                if (!approach.TryGetValue(cellId, out var distance) || distance >= currentDistance)
                    continue;

                var score = (currentDistance - distance) * 60 - RiskEvaluator.ScoreCellRisk(context, cellId, true);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        // Heuristica de respaldo con distancia Manhattan, para cuando el objetivo esta en una
        // region no transitable desde aqui (arenas partidas) y el BFS de aproximacion no llega.
        private static int? GetBestCellNearEnemyByGoalDistance(AIContext context, AbstractFighter target)
        {
            var currentDistance = context.TurnCache.Cells.GetDistance(context.CurrentCellId, target.Cell.Id);
            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId)
                    continue;

                var distance = context.TurnCache.Cells.GetDistance(cellId, target.Cell.Id);
                if (distance >= currentDistance)
                    continue;

                var score = (currentDistance - distance) * 60 - RiskEvaluator.ScoreCellRisk(context, cellId, true);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        public int? GetBestCellAwayFromEnemies(AIContext context)
        {
            if (!CanMove(context))
                return null;

            if (context?.Enemies == null || context.Fighter?.Cell == null || context.CurrentMP <= 0)
                return null;

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId)
                    continue;

                var nearest = context.Enemies.Where(e => e?.Cell != null).Select(e => context.TurnCache.Cells.GetDistance(cellId, e.Cell.Id)).DefaultIfEmpty(0).Min();

                var score = nearest * 70 - RiskEvaluator.ScoreCellRisk(context, cellId, false);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        public int? GetBestCellForDistanceAttack(AIContext context)
        {
            if (!CanMove(context))
                return null;

            if (context?.Enemies == null || context.SpellBook?.DamageSpells == null || context.CurrentMP <= 0)
                return null;



            var castableSpells = GetCastableDamageSpells(context);
            if (castableSpells.Count == 0)
                return null;

            // El danio estimado solo depende del hechizo: se precalcula una vez en lugar de
            // recomputarlo dentro del bucle celda x hechizo x enemigo.
            var spellDamage = new Dictionary<SpellLevel, int>(castableSpells.Count);
            foreach (var spell in castableSpells)
                spellDamage[spell] = SpellEvaluator.EstimateDamage(spell);

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId)
                    continue;

                foreach (var spell in castableSpells)
                {
                    foreach (var enemy in context.Enemies)
                    {
                        if (enemy?.Cell == null || enemy.IsFighterDead)
                            continue;

                        if (!SpellEvaluator.CanReachCell(context, spell, cellId, enemy.Cell.Id))
                            continue;




                        var currentDist = context.TurnCache.Cells.GetDistance(context.CurrentCellId, enemy.Cell.Id);
                        var newDist = context.TurnCache.Cells.GetDistance(cellId, enemy.Cell.Id);
                        var proximityBonus = System.Math.Max(0, currentDist - newDist) * 15;

                        var score = 120 + spellDamage[spell] + TargetEvaluator.ScoreLowHp(enemy) + proximityBonus - RiskEvaluator.ScoreCellRisk(context, cellId, false);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCell = cellId;
                        }
                    }
                }
            }

            return bestCell;
        }

        public int? GetBestCellForAggressiveApproach(AIContext context)
        {
            if (!CanMove(context))
                return null;

            if (context?.Enemies == null || context.SpellBook?.DamageSpells == null || context.CurrentMP <= 0)
                return null;

            var castableSpells = GetCastableDamageSpells(context);
            if (castableSpells.Count == 0)
                return null;

            int? bestCell = null;
            var bestEnemyDist = context.EnemyTargets?.Count > 0 ? context.EnemyTargets[0].Distance : int.MaxValue;

            var origin = context.CurrentCellId;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == origin)
                    continue;

                foreach (var spell in castableSpells)
                {
                    foreach (var enemy in context.Enemies)
                    {
                        if (enemy?.Cell == null || enemy.IsFighterDead)
                            continue;

                        if (!SpellEvaluator.CanReachCell(context, spell, cellId, enemy.Cell.Id))
                            continue;


                        var dist = context.TurnCache.Cells.GetDistance(cellId, enemy.Cell.Id);
                        if (dist < bestEnemyDist)
                        {
                            bestEnemyDist = dist;
                            bestCell = cellId;
                        }
                    }
                }
            }


            return bestCell;
        }

        public int? GetBestCellToCastSpell(AIContext context, SpellLevel spell, AbstractFighter target)
        {
            if (!CanMove(context))
                return null;

            if (context == null || spell == null || target?.Cell == null || context.CurrentMP <= 0)
                return null;

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId)
                    continue;

                if (!SpellEvaluator.CanCastFromCell(context, spell, cellId, target.Cell.Id))
                    continue;

                var distance = context.TurnCache.Cells.GetDistance(cellId, target.Cell.Id);
                var score = 100 - distance * 5 - RiskEvaluator.ScoreCellRisk(context, cellId, false);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        public IReadOnlyList<int> GetSafeReachableCells(AIContext context)
        {
            if (context == null || !CanMove(context))
                return new List<int>();

            return context.TurnCache.Cells.GetReachableCells().Where(cell => RiskEvaluator.ScoreCellRisk(context, cell, false) < 160).ToList();
        }

        public int? GetBestSummonCell(AIContext context, SpellLevel spell)
        {
            if (context?.Fighter?.Cell == null || context.Fight?.Map == null || spell == null)
                return null;

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in CellZone.GetCircleCells(context.Fight.Map, context.CurrentCellId, System.Math.Max(1, spell.MaxPO)))
            {
                var fightCell = context.Fight.GetCell(cellId);
                if (fightCell == null || !fightCell.CanWalk)
                    continue;

                if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, cellId))
                    continue;

                var nearestEnemyDistance = context.Enemies.Where(e => e?.Cell != null).Select(e => context.TurnCache.Cells.GetDistance(cellId, e.Cell.Id)).DefaultIfEmpty(0).Min();

                var score = 100 - nearestEnemyDistance * 4 - RiskEvaluator.ScoreCellRisk(context, cellId, true);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        public int? GetBestCellForPreferredDistance(AIContext context, AbstractFighter target, int minDistance, int maxDistance)
        {
            if (!CanMove(context))
                return null;

            if (context?.Fighter?.Cell == null || target?.Cell == null || context.CurrentMP <= 0)
                return null;

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId)
                    continue;

                var distance = context.TurnCache.Cells.GetDistance(cellId, target.Cell.Id);
                var inBand = distance >= minDistance && distance <= maxDistance;
                var score = inBand ? 160 : 80 - System.Math.Min(System.Math.Abs(distance - minDistance), System.Math.Abs(distance - maxDistance)) * 20;
                score -= RiskEvaluator.ScoreCellRisk(context, cellId, false);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        private static bool HasSingleRangedEnemy(AIContext context)
        {
            if (context?.Enemies == null) return false;
            var live = context.Enemies.Where(e => e != null && !e.IsFighterDead).ToList();
            if (live.Count != 1) return false;
            return live[0] is CharacterEntity ch && AIBreedProfile.IsRanged(ch.Breed);
        }

        public int? GetBestCellForMaxApproach(AIContext context)
        {
            if (!CanMove(context)) return null;

            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell == null) return null;

            // Distancia real de marcha cuando el objetivo es alcanzable; Manhattan como respaldo.
            var approach = context.TurnCache.Cells.GetApproachDistances(target.Cell.Id);
            var useApproach = approach.TryGetValue(context.CurrentCellId, out var bestDist);
            if (!useApproach)
                bestDist = context.TurnCache.Cells.GetDistance(context.CurrentCellId, target.Cell.Id);

            int? bestCell = null;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId) continue;

                int dist;
                if (useApproach)
                {
                    if (!approach.TryGetValue(cellId, out dist))
                        continue;
                }
                else
                {
                    dist = context.TurnCache.Cells.GetDistance(cellId, target.Cell.Id);
                }

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        private static bool CanMove(AIContext context)
        {
            return context?.Fighter != null && context.CurrentMP > 0 && context.Fighter.CanBeMoved();
        }

        // ¿La celda tiene un glifo del equipo contrario (que golpeara al empezar el turno aqui)?
        public static bool IsOnHostileGlyph(AIContext context, int cellId)
        {
            var fightCell = context?.Fight?.GetCell(cellId);
            if (fightCell == null || context.Fighter == null)
                return false;

            foreach (var obstacle in fightCell.FightObjects)
            {
                if (obstacle.ObstacleType != FightObstacleTypeEnum.TYPE_GLYPH)
                    continue;

                if (obstacle is AbstractActivableObject activable
                    && activable.Caster?.Team != null
                    && activable.Caster.Team != context.Fighter.Team)
                    return true;
            }

            return false;
        }

        // Mejor celda alcanzable SIN glifo hostil, para bajarse del que se esta pisando. Se
        // prioriza la de menor riesgo y, a igualdad, la mas cercana al enemigo (seguir siendo
        // util). Devuelve null si toda celda alcanzable tiene glifo hostil (no hay a donde ir).
        public int? GetBestCellOffHostileGlyph(AIContext context)
        {
            if (!CanMove(context))
                return null;

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId || IsOnHostileGlyph(context, cellId))
                    continue;

                var nearest = context.Enemies == null ? 0 : context.Enemies
                    .Where(e => e?.Cell != null && !e.IsFighterDead)
                    .Select(e => context.TurnCache.Cells.GetDistance(cellId, e.Cell.Id))
                    .DefaultIfEmpty(0).Min();

                var score = -RiskEvaluator.ScoreCellRisk(context, cellId, IsMeleeOnly(context)) - nearest;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cellId;
                }
            }

            return bestCell;
        }

        private static List<SpellLevel> GetCastableDamageSpells(AIContext context)
        {
            var result = new List<SpellLevel>();
            foreach (var spell in context.SpellBook.DamageSpells)
            {
                if (spell != null && spell.APCost <= context.CurrentAP && SpellEvaluator.CanCastSpellThisTurn(context, spell))
                    result.Add(spell);
            }

            return result;
        }
    }
}
