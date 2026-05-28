using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Sram — Assassin / trap specialist.
    ///
    /// Priority pipeline:
    ///   1. Place traps near target path (High)
    ///   2. AP/MP-removal debuffs (High)
    ///   3. Kill shot (Critical)
    ///   4. Attack weakest target
    ///   5. Tactical approach
    ///
    /// NOTE: Sram invisibility requires knowing the exact FighterStateEnum camo ID.
    /// TODO: Add invisibility logic once FighterStateEnum for Sram camo state is confirmed.
    /// Spells are detected by effect type — no hardcoded spell IDs used.
    /// </summary>
    public sealed class DopeulSramBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.DamageMelee;
        protected override int         PreferredMinDistance => 1;
        protected override int         PreferredMaxDistance => 4;
        protected override bool        PrioritizeDebuff     => true;

        public DopeulSramBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();

            // 1. Traps — place near nearest enemy path
            if (context.SpellBook?.TrapSpells?.Count > 0)
            {
                var trapTarget = TargetEvaluator.GetNearestEnemy(context);
                if (trapTarget?.Cell != null)
                {
                    foreach (var spell in context.SpellBook.TrapSpells)
                    {
                        if (spell == null || spell.APCost > context.CurrentAP)
                            continue;

                        // Try to place on target cell or best adjacent cell
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
                            Type     = AIDecisionType.CastSpell,
                            Priority = AIDecisionPriority.High,
                            Score    = 150 + areaScore,
                            SpellId  = spell.SpellId,
                            TargetId = null,
                            CellId   = (short)trapCell.Value,
                            Reason   = "Sram placing trap"
                        };
                    }
                }
            }

            // 2. AP/MP-removal debuffs
            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                decision.Score    += 100;
                decision.Priority  = AIDecisionPriority.High;
                yield return decision;
            }

            // 3. Kill shot
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 4. Attack weakest target
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                var enemy = context.Enemies?.FirstOrDefault(e => e?.Id == decision.TargetId);
                decision.Score += 70 + (enemy != null ? TargetEvaluator.ScoreLowHp(enemy) / 2 : 0);
                yield return decision;
            }

            // 5. Tactical approach
            var nearCell = movement.GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
                yield return AIDecision.Move(nearCell.Value, 100, AIDecisionPriority.Low, "Sram tactical approach");
        }
    }
}
