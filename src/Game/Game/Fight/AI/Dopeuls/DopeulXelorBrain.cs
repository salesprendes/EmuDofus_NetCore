using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Xelor — AP controller + teleporter.
    ///
    /// Priority pipeline:
    ///   1. AP-removal debuffs on most dangerous enemy (Critical)
    ///   2. Other debuffs
    ///   3. Teleport / reposition spells (Normal) — only if still has AP left
    ///   4. Kill shot (Critical)
    ///   5. Attack from distance
    ///   6. Preferred distance
    ///
    /// Spells detected by effect type — no hardcoded IDs.
    /// </summary>
    public sealed class DopeulXelorBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.Controller;
        protected override int         PreferredMinDistance => 3;
        protected override int         PreferredMaxDistance => 7;
        protected override bool        PrioritizeDebuff     => true;

        public DopeulXelorBrain(AIFighter fighter) : base(fighter) {}

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement      = new MovementEvaluator();
            var mostDangerous = TargetEvaluator.GetMostDangerousEnemy(context);

            // 1 & 2. Debuffs — AP removal on most dangerous target gets critical bonus
            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                var spell      = context.SpellBook?.AllSpells?.FirstOrDefault(s => s?.SpellId == decision.SpellId);
                var isAPRemoval = spell != null && AISpellBook.HasRemoveAPEffect(spell);
                var isDangerous = mostDangerous != null && decision.TargetId == mostDangerous.Id;

                if (isAPRemoval)
                {
                    decision.Score    += isDangerous ? 250 : 160;
                    decision.Priority  = isDangerous ? AIDecisionPriority.Critical : AIDecisionPriority.High;
                    decision.Reason    = "Xelor AP removal" + (isDangerous ? " (priority target)" : "");
                }
                else
                {
                    decision.Score += 60;
                }
                yield return decision;
            }

            // 3. Teleport / repositioning spells — cast on self when still castable
            if (context.SpellBook?.MovementSpells?.Count > 0)
            {
                foreach (var spell in context.SpellBook.MovementSpells)
                {
                    if (spell == null || spell.APCost > context.CurrentAP)
                        continue;

                    // Only suggest self-teleport from current cell if it passes range check
                    if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, context.CurrentCellId))
                        continue;

                    yield return new AIDecision
                    {
                        Type     = AIDecisionType.CastSpell,
                        Priority = AIDecisionPriority.Normal,
                        Score    = 90,
                        SpellId  = spell.SpellId,
                        TargetId = context.Fighter?.Id,
                        CellId   = (short)context.CurrentCellId,
                        Reason   = "Xelor repositioning"
                    };
                }
            }

            // 4. Kill shot
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 5. Attack from distance
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 40;
                yield return decision;
            }

            // 6. Preferred distance
            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(
                    context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Xelor preferred distance");
            }
        }
    }
}
