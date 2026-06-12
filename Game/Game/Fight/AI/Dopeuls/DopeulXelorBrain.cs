using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    public sealed class DopeulXelorBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role => DopeulRole.Controller;
        protected override int PreferredMinDistance => 3;
        protected override int PreferredMaxDistance => 7;
        protected override bool PrioritizeDebuff => true;

        public DopeulXelorBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();
            var mostDangerous = TargetEvaluator.GetMostDangerousEnemy(context);


            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                var spell = context.SpellBook?.AllSpells?.FirstOrDefault(s => s?.SpellId == decision.SpellId);
                var isAPRemoval = spell != null && AISpellBook.HasRemoveAPEffect(spell);
                var isDangerous = mostDangerous != null && decision.TargetId == mostDangerous.Id;

                if (isAPRemoval)
                {
                    decision.Score += isDangerous ? 250 : 160;
                    decision.Priority = isDangerous ? AIDecisionPriority.Critical : AIDecisionPriority.High;
                    decision.Reason = "Xelor AP removal" + (isDangerous ? " (priority target)" : "");
                }
                else
                {
                    decision.Score += 60;
                }
                yield return decision;
            }


            if (context.SpellBook?.MovementSpells?.Count > 0)
            {
                foreach (var spell in context.SpellBook.MovementSpells)
                {
                    if (spell == null || spell.APCost > context.CurrentAP)
                        continue;


                    if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, context.CurrentCellId))
                        continue;

                    yield return new AIDecision
                    {
                        Type = AIDecisionType.CastSpell,
                        Priority = AIDecisionPriority.Normal,
                        Score = 90,
                        SpellId = spell.SpellId,
                        TargetId = context.Fighter?.Id,
                        CellId = (short)context.CurrentCellId,
                        Reason = "Xelor repositioning"
                    };
                }
            }


            foreach (var decision in GetKillDecisions(context))
                yield return decision;


            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 40;
                yield return decision;
            }


            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Xelor distancia preferida");
            }
        }
    }
}
