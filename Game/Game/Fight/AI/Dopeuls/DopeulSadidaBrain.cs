using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    public sealed class DopeulSadidaBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role => DopeulRole.Summoner;
        protected override int PreferredMinDistance => 3;
        protected override int PreferredMaxDistance => 7;
        protected override bool PrioritizeDebuff => true;

        public DopeulSadidaBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {

            foreach (var decision in new SummonEvaluator().Evaluate(context))
            {
                decision.Score += 160;
                decision.Priority = AIDecisionPriority.High;
                yield return decision;
            }


            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                var spell = context.SpellBook?.AllSpells?.FirstOrDefault(s => s?.SpellId == decision.SpellId);
                if (spell != null && AISpellBook.HasRemoveMPEffect(spell))
                {
                    decision.Score += 120;
                    decision.Priority = AIDecisionPriority.High;
                    decision.Reason = "Sadida MP removal";
                }
                else
                {
                    decision.Score += 50;
                }
                yield return decision;
            }


            foreach (var decision in GetKillDecisions(context))
                yield return decision;


            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 40;
                yield return decision;
            }


            var movement = new MovementEvaluator();
            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Sadida distancia preferida");
            }
        }
    }
}
