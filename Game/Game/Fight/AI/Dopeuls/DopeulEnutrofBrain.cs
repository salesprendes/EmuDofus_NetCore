using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Enutrof — Debuffer + Summoner.
    ///
    /// Priority pipeline:
    ///   1. MP-removal on most dangerous enemy (Critical / High)
    ///   2. Other debuffs
    ///   3. Summon (Critical/High)
    ///   4. Kill shot (Critical)
    ///   5. Attack
    ///   6. Preferred distance
    ///
    /// FIX: PrioritizeDebuff=true was missing from the original stub.
    /// </summary>
    public sealed class DopeulEnutrofBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.Debuffer;
        protected override int         PreferredMinDistance => 3;
        protected override int         PreferredMaxDistance => 7;
        protected override bool        PrioritizeSummon     => true;
        protected override bool        PrioritizeDebuff     => true;   // was missing in stub

        public DopeulEnutrofBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var mostDangerous = TargetEvaluator.GetMostDangerousEnemy(context);

            // 1 & 2. Debuffs — MP removal on the most dangerous enemy gets critical bonus
            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                var spell      = context.SpellBook?.AllSpells?.FirstOrDefault(s => s?.SpellId == decision.SpellId);
                var isMPRemoval = spell != null && AISpellBook.HasRemoveMPEffect(spell);
                var isDangerous = mostDangerous != null && decision.TargetId == mostDangerous.Id;

                if (isMPRemoval)
                {
                    decision.Score    += isDangerous ? 200 : 130;
                    decision.Priority  = isDangerous ? AIDecisionPriority.Critical : AIDecisionPriority.High;
                    decision.Reason    = "Enutrofa MP removal" + (isDangerous ? " (priority target)" : "");
                }
                else
                {
                    decision.Score += 60;
                }
                yield return decision;
            }

            // 3. Summon (e.g. treasure chest)
            foreach (var decision in new SummonEvaluator().Evaluate(context))
            {
                decision.Score    += 130;
                decision.Priority  = AIDecisionPriority.High;
                yield return decision;
            }

            // 4. Kill shot
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 5. Attack
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 40;
                yield return decision;
            }

            // 6. Preferred distance
            var movement = new MovementEvaluator();
            var target   = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(
                    context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Enutrofa preferred distance");
            }
        }
    }
}
