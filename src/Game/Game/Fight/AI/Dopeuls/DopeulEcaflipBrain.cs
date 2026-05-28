using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Ecaflip — Hybrid damage dealer.
    ///
    /// Priority pipeline:
    ///   1. Kill shot (Critical) — capitalises on low-HP targets
    ///   2. Attack with kill-chance bonus
    ///   3. Self-buff (Normal) — improves subsequent damage
    ///   4. Optimal positioning for mid-range attacks
    ///
    /// NOTE: No random logic — all decisions are deterministic and score-based.
    /// Ecaflip does not call RNG; chance is handled by the server's spell resolution.
    /// </summary>
    public sealed class DopeulEcaflipBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.Hybrid;
        protected override int         PreferredMinDistance => 2;
        protected override int         PreferredMaxDistance => 6;
        protected override bool        PrioritizeBuff       => true;

        public DopeulEcaflipBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();

            // 1. Kill shot
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 2. Attack — boosted by kill chance
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                var spell     = context.SpellBook?.AllSpells?.FirstOrDefault(s => s?.SpellId == decision.SpellId);
                var enemy     = context.Enemies?.FirstOrDefault(e => e?.Id == decision.TargetId);
                var killBonus = (spell != null && enemy != null)
                    ? TargetEvaluator.ScoreKillChance(context.Fighter, enemy, SpellEvaluator.EstimateDamage(spell)) / 4
                    : 0;
                decision.Score += 60 + killBonus;
                yield return decision;
            }

            // 3. Self-buff
            foreach (var decision in new BuffEvaluator().Evaluate(context))
            {
                if (decision.TargetId == context.Fighter?.Id)
                {
                    decision.Score    += 90;
                    decision.Priority  = AIDecisionPriority.Normal;
                    decision.Reason    = "Ecaflip self-buff";
                }
                yield return decision;
            }

            // 4. Position for mid-range
            var target = TargetEvaluator.GetWeakestEnemy(context) ?? TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(
                    context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 110, AIDecisionPriority.Low, "Ecaflip positioning");
            }
        }
    }
}
