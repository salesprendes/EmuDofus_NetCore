using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Profiles
{
    public class PassiveBrain : AIBrain
    {
        public PassiveBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var move = new MovementEvaluator().GetBestCellNearEnemy(context);
            if (move.HasValue)
                yield return AIDecision.Move(move.Value, 30, AIDecisionPriority.Low, "Passive blocking movement");

            yield return AIDecision.EndTurn("Passive profile");
        }
    }
}
