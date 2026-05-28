using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Profiles
{
    public sealed class SummonerBrain : AIBrain
    {
        public SummonerBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            foreach (var decision in new SummonEvaluator().Evaluate(context))
            {
                decision.Score += 140;
                decision.Priority = AIDecisionPriority.High;
                yield return decision;
            }

            foreach (var decision in new BuffEvaluator().Evaluate(context))
                yield return decision;

            foreach (var decision in new AttackEvaluator().Evaluate(context))
                yield return decision;

            foreach (var decision in new MovementEvaluator().Evaluate(context))
                yield return decision;
        }
    }
}
