using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Profiles
{
    public sealed class AggressiveBrain : AIBrain
    {
        public AggressiveBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {

            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 90;
                if (decision.Priority == AIDecisionPriority.Normal)
                    decision.Priority = AIDecisionPriority.High;
                yield return decision;
            }

            var movement = new MovementEvaluator();








            var aggressiveCell = movement.GetBestCellForAggressiveApproach(context);
            if (aggressiveCell.HasValue)
                yield return AIDecision.Move(aggressiveCell.Value, 150, AIDecisionPriority.High, "Agresivo se mueve para atacar");






            var nearCell = movement.GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
                yield return AIDecision.Move(nearCell.Value, 100, AIDecisionPriority.Low, "Acercamiento agresivo");
        }
    }
}
