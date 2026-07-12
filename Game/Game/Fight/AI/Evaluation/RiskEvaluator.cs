using Game.Fight.AI.Cache;
using Game.Fight.AI.Core;
using Game.Fight;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    // Helpers de evaluacion de riesgo de casilla (amenaza enemiga, glifos). No es un evaluador:
    // lo usan los evaluadores reales por sus metodos estaticos.
    public static class RiskEvaluator
    {
        public static int ScoreCellRisk(AIContext context, int cellId, bool meleeOrTank = false)
        {
            if (context?.Enemies == null || cellId < 0)
                return 0;


            var cache = context.TurnCache?.CellRiskScores;
            var cacheKey = (cellId << 1) | (meleeOrTank ? 1 : 0);
            if (cache != null && cache.TryGetValue(cacheKey, out var cachedPenalty))
                return cachedPenalty;

            var penalty = 0;
            var adjacentEnemies = 0;
            var nearestDistance = int.MaxValue;

            foreach (var enemy in context.Enemies)
            {
                if (enemy?.Cell == null || enemy.IsFighterDead)
                    continue;

                var distance = context.TurnCache.Cells.GetDistance(cellId, enemy.Cell.Id);
                if (distance < nearestDistance)
                    nearestDistance = distance;
                if (distance <= 1)
                    adjacentEnemies++;
            }

            if (adjacentEnemies > 0 && !meleeOrTank)
                penalty += adjacentEnemies * 120;
            else if (adjacentEnemies > 1)
                penalty += adjacentEnemies * 35;

            if (!meleeOrTank && nearestDistance <= 2)
                penalty += 40;

            // Anti-apiñamiento: pegarse a los aliados regala golpes en área al enemigo. Los
            // cuerpo a cuerpo lo penalizan menos (inevitablemente se agrupan sobre el objetivo).
            if (context.Allies != null)
            {
                foreach (var ally in context.Allies)
                {
                    if (ally == null || ally == context.Fighter || ally.Cell == null || ally.IsFighterDead)
                        continue;

                    if (context.TurnCache.Cells.GetDistance(cellId, ally.Cell.Id) <= 1)
                        penalty += meleeOrTank ? 6 : 15;
                }
            }

            // Penaliza pisar glifos hostiles (son visibles). Las trampas enemigas estan ocultas,
            // asi que la IA no las "ve" y no las esquiva (seria hacer trampa).
            var fightCell = context.Fight?.GetCell(cellId);
            if (fightCell != null)
            {
                foreach (var obstacle in fightCell.FightObjects)
                {
                    if (obstacle.ObstacleType != FightObstacleTypeEnum.TYPE_GLYPH)
                        continue;

                    if (obstacle is AbstractActivableObject activable
                        && activable.Caster?.Team != null
                        && activable.Caster.Team != context.Fighter.Team)
                    {
                        penalty += 90;
                    }
                }
            }

            // Amenaza a distancia: un enemigo lejano pero con PM + alcance suficiente (y linea de
            // vision) puede golpear esta celda en su turno. Penaliza para que la IA a distancia
            // busque celdas fuera del alcance enemigo, no solo lejos del cuerpo a cuerpo.
            if (!meleeOrTank)
            {
                foreach (var threat in GetEnemyThreats(context))
                {
                    if (threat?.Fighter?.Cell == null)
                        continue;

                    var distance = context.TurnCache.Cells.GetDistance(cellId, threat.Fighter.Cell.Id);
                    if (distance > threat.Reach)
                        continue;

                    // Los enemigos a distancia necesitan linea de vision para alcanzar la celda. Se
                    // mide con la IA ya situada en ella: es el blanco que el enemigo tendria delante.
                    if (threat.Ranged && !context.TurnCache.LineOfSight.HasLineOfSight(threat.Fighter.Cell.Id, cellId, cellId))
                        continue;

                    penalty += 20 + Math.Min(threat.Damage, 240) / 4;
                }
            }

            if (cache != null)
                cache[cacheKey] = penalty;

            return penalty;
        }

        public static bool IsSafeForRanged(AIContext context, int cellId)
        {
            return ScoreCellRisk(context, cellId, false) < 120;
        }

        // Construye (y cachea por turno) el perfil de amenaza de cada enemigo: hasta donde puede
        // golpear este turno (PM + alcance del hechizo de danio) y cuanto danio nos haria.
        public static IReadOnlyList<AIEnemyThreat> GetEnemyThreats(AIContext context)
        {
            if (context?.TurnCache == null)
                return Array.Empty<AIEnemyThreat>();

            if (context.TurnCache.EnemyThreats != null)
                return context.TurnCache.EnemyThreats;

            var threats = new List<AIEnemyThreat>();
            if (context.Enemies != null)
            {
                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                        continue;

                    var range = 1;
                    var damage = 0;
                    var bonusRange = enemy.Statistics?.GetTotal(EffectEnum.STAT_MAS_ALCANCE) ?? 0;

                    var spells = enemy.SpellBook?.GetSpells();
                    if (spells != null)
                    {
                        foreach (var spell in spells)
                        {
                            if (spell == null || !AISpellBook.HasDamageEffect(spell))
                                continue;

                            var maxPo = spell.AllowPOBoost && spell.MaxPO != 0 ? spell.MaxPO + bonusRange : spell.MaxPO;
                            var spellRange = Math.Max(spell.MinPO, maxPo);
                            if (spellRange > range)
                                range = spellRange;

                            var spellDamage = SpellEvaluator.EstimateDamageOnTarget(enemy, spell, context.Fighter);
                            if (spellDamage > damage)
                                damage = spellDamage;
                        }
                    }

                    var reach = (enemy.MP > 0 ? enemy.MP : 0) + range;
                    threats.Add(new AIEnemyThreat(enemy, reach, damage, range > 1));
                }
            }

            context.TurnCache.EnemyThreats = threats;
            return threats;
        }
    }
}
