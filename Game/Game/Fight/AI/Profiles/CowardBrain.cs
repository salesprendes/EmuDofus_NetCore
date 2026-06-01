using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Profiles
{
    public sealed class CowardBrain : AIBrain
    {
        public CowardBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();
            var awayCell = movement.GetBestCellAwayFromEnemies(context);
            if (awayCell.HasValue)
                yield return AIDecision.Move(awayCell.Value, 220, AIDecisionPriority.High, "Coward escape");

            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                if (RiskEvaluator.ScoreCellRisk(context, context.CurrentCellId, false) < 100)
                {
                    decision.Score -= 40;
                    yield return decision;
                }
            }

            yield return AIDecision.EndTurn("No safe coward action");
        }
    }
}
