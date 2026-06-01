using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Profiles
{
    public sealed class DistanceBrain : AIBrain
    {
        private const int PreferredMinDistance = 4;
        private const int PreferredMaxDistance = 9;

        public DistanceBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                decision.Score += 30;
                yield return decision;
            }

            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 40;
                yield return decision;
            }

            var movement = new MovementEvaluator();
            var nearest = TargetEvaluator.GetNearestEnemy(context);
            if (nearest?.Cell != null)
            {
                var distance = context.TurnCache.Cells.GetDistance(context.CurrentCellId, nearest.Cell.Id);
                if (distance < PreferredMinDistance)
                {
                    var awayCell = movement.GetBestCellAwayFromEnemies(context);
                    if (awayCell.HasValue)
                        yield return AIDecision.Move(awayCell.Value, 180, AIDecisionPriority.High, "Keep distance");
                }

                var preferredCell = movement.GetBestCellForPreferredDistance(context, nearest, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Preferred ranged distance");
            }

            var castCell = movement.GetBestCellForDistanceAttack(context);
            if (castCell.HasValue)
                yield return AIDecision.Move(castCell.Value, 130, AIDecisionPriority.Normal, "Ranged casting cell");
        }
    }
}
