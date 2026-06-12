using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    public sealed class DopeulEnutrofBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role => DopeulRole.Debuffer;
        protected override int PreferredMinDistance => 3;
        protected override int PreferredMaxDistance => 7;
        protected override bool PrioritizeSummon => true;
        protected override bool PrioritizeDebuff => true;

        public DopeulEnutrofBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var mostDangerous = TargetEvaluator.GetMostDangerousEnemy(context);


            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                var spell = context.SpellBook?.AllSpells?.FirstOrDefault(s => s?.SpellId == decision.SpellId);
                var isMPRemoval = spell != null && AISpellBook.HasRemoveMPEffect(spell);
                var isDangerous = mostDangerous != null && decision.TargetId == mostDangerous.Id;

                if (isMPRemoval)
                {
                    decision.Score += isDangerous ? 200 : 130;
                    decision.Priority = isDangerous ? AIDecisionPriority.Critical : AIDecisionPriority.High;
                    decision.Reason = "Enutrofa MP removal" + (isDangerous ? " (priority target)" : "");
                }
                else
                {
                    decision.Score += 60;
                }
                yield return decision;
            }


            foreach (var decision in new SummonEvaluator().Evaluate(context))
            {
                decision.Score += 130;
                decision.Priority = AIDecisionPriority.High;
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
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Enutrofa distancia preferida");
            }
        }
    }
}
