using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Profiles
{
    public sealed class HealerBrain : AIBrain
    {
        public HealerBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            foreach (var decision in new HealEvaluator().Evaluate(context))
            {
                decision.Score += 120;
                yield return decision;
            }

            foreach (var decision in new BuffEvaluator().Evaluate(context))
            {
                decision.Score += 40;
                yield return decision;
            }

            foreach (var decision in new AttackEvaluator().Evaluate(context))
                yield return decision;

            var awayCell = new MovementEvaluator().GetBestCellAwayFromEnemies(context);
            if (awayCell.HasValue)
                yield return AIDecision.Move(awayCell.Value, 80, AIDecisionPriority.Low, "Healer safe distance");
        }
    }
}
