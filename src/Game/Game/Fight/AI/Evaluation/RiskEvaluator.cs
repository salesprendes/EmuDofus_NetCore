using Game.Fight.AI.Core;
using Game.Fight;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    public sealed class RiskEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            yield break;
        }

        public static int ScoreCellRisk(AIContext context, int cellId, bool meleeOrTank = false)
        {
            if (context?.Enemies == null || cellId < 0)
                return 0;

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

            return penalty;
        }

        public static bool IsSafeForRanged(AIContext context, int cellId)
        {
            return ScoreCellRisk(context, cellId, false) < 120;
        }
    }
}
