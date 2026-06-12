using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    public sealed class DopeulSramBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role => DopeulRole.DamageMelee;
        protected override int PreferredMinDistance => 1;
        protected override int PreferredMaxDistance => 4;
        protected override bool PrioritizeDebuff => true;

        public DopeulSramBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();


            if (context.SpellBook?.TrapSpells?.Count > 0)
            {
                var trapTarget = TargetEvaluator.GetNearestEnemy(context);
                if (trapTarget?.Cell != null)
                {
                    foreach (var spell in context.SpellBook.TrapSpells)
                    {
                        if (spell == null || spell.APCost > context.CurrentAP)
                            continue;


                        int? trapCell = null;
                        if (SpellEvaluator.CanCastFromCurrentCell(context, spell, trapTarget.Cell.Id))
                        {
                            trapCell = trapTarget.Cell.Id;
                        }
                        else
                        {
                            trapCell = movement.GetBestCellToCastSpell(context, spell, trapTarget);
                        }

                        if (!trapCell.HasValue)
                            continue;

                        var areaScore = SpellEvaluator.ScoreAreaImpact(context, spell, trapCell.Value, false);
                        yield return new AIDecision
                        {
                            Type = AIDecisionType.CastSpell,
                            Priority = AIDecisionPriority.High,
                            Score = 150 + areaScore,
                            SpellId = spell.SpellId,
                            TargetId = null,
                            CellId = (short)trapCell.Value,
                            Reason = "Sram placing trap"
                        };
                    }
                }
            }


            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                decision.Score += 100;
                decision.Priority = AIDecisionPriority.High;
                yield return decision;
            }


            foreach (var decision in GetKillDecisions(context))
                yield return decision;


            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                var enemy = context.Enemies?.FirstOrDefault(e => e?.Id == decision.TargetId);
                decision.Score += 70 + (enemy != null ? TargetEvaluator.ScoreLowHp(enemy) / 2 : 0);
                yield return decision;
            }


            var nearCell = movement.GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
                yield return AIDecision.Move(nearCell.Value, 100, AIDecisionPriority.Low, "Sram acercamiento tactico");
        }
    }
}
