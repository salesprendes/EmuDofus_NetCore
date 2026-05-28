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
                        yield return AIDecision.Move(fleeCell.Value, 120, AIDecisionPriority.Low, "Melee flee — low HP");
                    yield break;
                }

                if (HasSingleRangedEnemy(context))
                {
                    var forcedCell = GetBestCellForMaxApproach(context);
                    if (forcedCell.HasValue)
                    {
                        yield return AIDecision.Move(forcedCell.Value, 120, AIDecisionPriority.Low, "Force approach vs ranged enemy");
                        yield break;
                    }
                }

                var meleeAttackCell = GetBestCellForAggressiveApproach(context);
                if (meleeAttackCell.HasValue)
                {
                    yield return AIDecision.Move(meleeAttackCell.Value, 100, AIDecisionPriority.Low, "Melee approach to attack");
                    yield break;
                }

                var meleeNearCell = GetBestCellNearEnemy(context);
                if (meleeNearCell.HasValue)
                    yield return AIDecision.Move(meleeNearCell.Value, 60, AIDecisionPriority.Low, "Close in for melee");
            }
            else
            {
                var attackCell = GetBestCellForDistanceAttack(context);
                if (attackCell.HasValue)
                    yield return AIDecision.Move(attackCell.Value, 100, AIDecisionPriority.Low, "Move to cast a spell");

                var nearCell = GetBestCellNearEnemy(context);
                if (nearCell.HasValue)
                    yield return AIDecision.Move(nearCell.Value, 60, AIDecisionPriority.Low, "Move closer to target");
            }
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

            var maxPo = spell.AllowPOBoost && spell.MaxPO != 0 && context?.Fighter?.Statistics != null
                ? spell.MaxPO + context.Fighter.Statistics.GetTotal(EffectEnum.AddPO)
                : spell.MaxPO;

            return System.Math.Max(spell.MinPO, maxPo);
        }

        private static bool IsSelfLowHP(AIContext context, double threshold)
        {
            var f = context?.Fighter;
            return f != null && f.MaxLife > 0 && (double)f.Life / f.MaxLife < threshold;
        }

        public int? GetBestCellNearEnemy(AIContext context)
        {
            // Always target the NEAREST enemy for movement direction.
            // Using GetWeakestEnemy caused the fighter to drift toward a low-HP but distant
            // summon and stop there, wasting PM and never reaching the main threat.
            if (!CanMove(context))
                return null;

            var target = TargetEvaluator.GetNearestEnemy(context);
            if (context?.Fighter?.Cell == null || target?.Cell == null || context.CurrentMP <= 0)
                return null;

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

                var nearest = context.Enemies
                    .Where(e => e?.Cell != null)
                    .Select(e => context.TurnCache.Cells.GetDistance(cellId, e.Cell.Id))
                    .DefaultIfEmpty(0)
                    .Min();

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

            int? bestCell = null;
            var bestScore = int.MinValue;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId)
                    continue;

                foreach (var spell in context.SpellBook.DamageSpells)
                {
                    if (spell == null || spell.APCost > context.CurrentAP)
                        continue;

                    foreach (var enemy in context.Enemies)
                    {
                        if (enemy?.Cell == null || enemy.IsFighterDead)
                            continue;

                        if (!SpellEvaluator.CanCastFromCell(context, spell, cellId, enemy.Cell.Id))
                            continue;

                        // Proximity bonus: prefer cells that bring the fighter closer to the enemy.
                        // Without this, all castable cells score the same (120 + damage), so the
                        // function would pick arbitrarily — often a cell far from the enemy.
                        var currentDist  = context.TurnCache.Cells.GetDistance(context.CurrentCellId, enemy.Cell.Id);
                        var newDist      = context.TurnCache.Cells.GetDistance(cellId, enemy.Cell.Id);
                        var proximityBonus = System.Math.Max(0, currentDist - newDist) * 15;

                        var score = 120 + SpellEvaluator.EstimateDamage(spell)
                            + TargetEvaluator.ScoreLowHp(enemy)
                            + proximityBonus
                            - RiskEvaluator.ScoreCellRisk(context, cellId, false);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCell  = cellId;
                        }
                    }
                }
            }

            return bestCell;
        }

        /// <summary>
        /// Finds the reachable cell from which the fighter can cast a damage spell and that
        /// puts it as close as possible to an enemy — with NO risk penalty.
        /// Designed for aggressive brains that want to close in and attack at melee range.
        /// Returns null if no spell can be cast from any reachable cell that is closer than
        /// the current position.
        /// </summary>
        public int? GetBestCellForAggressiveApproach(AIContext context)
        {
            if (!CanMove(context))
                return null;

            if (context?.Enemies == null || context.SpellBook?.DamageSpells == null || context.CurrentMP <= 0)
                return null;

            int? bestCell = null;
            var bestEnemyDist = context.EnemyTargets?.Count > 0
                ? context.EnemyTargets[0].Distance   // current nearest-enemy distance
                : int.MaxValue;

            var origin = context.CurrentCellId;

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == origin)
                    continue;

                foreach (var spell in context.SpellBook.DamageSpells)
                {
                    if (spell == null || spell.APCost > context.CurrentAP)
                        continue;

                    foreach (var enemy in context.Enemies)
                    {
                        if (enemy?.Cell == null || enemy.IsFighterDead)
                            continue;

                        if (!SpellEvaluator.CanCastFromCell(context, spell, cellId, enemy.Cell.Id))
                            continue;

                        // Prefer cells that are closest to any enemy (minimum distance wins).
                        var dist = context.TurnCache.Cells.GetDistance(cellId, enemy.Cell.Id);
                        if (dist < bestEnemyDist)
                        {
                            bestEnemyDist = dist;
                            bestCell      = cellId;
                        }
                    }
                }
            }

            // Only return a cell if it is strictly closer to an enemy than the current position.
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

            return context.TurnCache.Cells.GetReachableCells()
                .Where(cell => RiskEvaluator.ScoreCellRisk(context, cell, false) < 160)
                .ToList();
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

                var nearestEnemyDistance = context.Enemies
                    .Where(e => e?.Cell != null)
                    .Select(e => context.TurnCache.Cells.GetDistance(cellId, e.Cell.Id))
                    .DefaultIfEmpty(0)
                    .Min();

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

        /// <summary>
        /// True when there is exactly one living enemy and that enemy is a ranged-class character.
        /// </summary>
        private static bool HasSingleRangedEnemy(AIContext context)
        {
            if (context?.Enemies == null) return false;
            var live = context.Enemies.Where(e => e != null && !e.IsFighterDead).ToList();
            if (live.Count != 1) return false;
            return live[0] is CharacterEntity ch && AIBreedProfile.IsRanged(ch.Breed);
        }

        /// <summary>
        /// Returns the reachable cell that minimises distance to the nearest enemy,
        /// with no risk penalty — pure aggressive approach using all available PM.
        /// </summary>
        public int? GetBestCellForMaxApproach(AIContext context)
        {
            if (!CanMove(context)) return null;

            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell == null) return null;

            int? bestCell = null;
            var bestDist = context.TurnCache.Cells.GetDistance(context.CurrentCellId, target.Cell.Id);

            foreach (var cellId in context.TurnCache.Cells.GetReachableCells())
            {
                if (cellId == context.CurrentCellId) continue;
                var dist = context.TurnCache.Cells.GetDistance(cellId, target.Cell.Id);
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
            return context?.Fighter != null
                && context.CurrentMP > 0
                && context.Fighter.CanBeMoved();
        }
    }
}
