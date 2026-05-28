using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Profiles
{
    public class DefaultBrain : AIBrain
    {
        public DefaultBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            foreach (var decision in RunEvaluators(context, new HealEvaluator(), new SummonEvaluator(), new BuffEvaluator(), new DebuffEvaluator(), new AttackEvaluator(), new MovementEvaluator()))
            {
                yield return decision;
            }
        }

        protected static IEnumerable<AIDecision> RunEvaluators(AIContext context, params IAIEvaluator[] evaluators)
        {
            if (evaluators == null)
            {
                yield break;
            }

            foreach (var evaluator in evaluators)
            {
                if (evaluator == null)
                {
                    continue;
                }

                var decisions = evaluator.Evaluate(context);
                if (decisions == null)
                {
                    continue;
                }

                foreach (var decision in decisions)
                {
                    if (decision != null)
                    {
                        yield return decision;
                    }
                }
            }
        }
    }
}
