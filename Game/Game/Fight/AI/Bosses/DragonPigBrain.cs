using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Bosses
{
    public sealed class DragonPigBrain : AIBrain
    {
        public DragonPigBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            foreach (var decision in new BuffEvaluator().Evaluate(context))
            {
                decision.Score += 60;
                decision.Priority = AIDecisionPriority.High;
                decision.Reason = "Dragon Pig usa bonificacion si esta disponible";
                yield return decision;
            }

            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 120;
                if (decision.Priority == AIDecisionPriority.Normal)
                {
                    decision.Priority = AIDecisionPriority.High;
                }

                yield return decision;
            }

            var nearCell = new MovementEvaluator().GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
            {
                yield return AIDecision.Move(nearCell.Value, 130, AIDecisionPriority.High, "Dragon Pig acercamiento agresivo");
            }
        }
    }
}
