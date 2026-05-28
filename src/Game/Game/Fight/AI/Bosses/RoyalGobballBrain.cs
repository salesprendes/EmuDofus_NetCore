using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Bosses
{
    public sealed class RoyalGobballBrain : AIBrain
    {
        public RoyalGobballBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            foreach (var decision in new HealEvaluator().Evaluate(context))
            {
                decision.Score += 80;
                yield return decision;
            }

            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 70;
                if (decision.Priority == AIDecisionPriority.Normal)
                    decision.Priority = AIDecisionPriority.High;
                yield return decision;
            }

            var nearCell = new MovementEvaluator().GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
                yield return AIDecision.Move(nearCell.Value, 110, AIDecisionPriority.Normal, "Royal Gobball melee approach");
        }
    }
}
