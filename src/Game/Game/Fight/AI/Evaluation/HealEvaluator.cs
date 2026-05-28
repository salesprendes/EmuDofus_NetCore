using Game.Fight.AI.Core;
using System.Collections.Generic;

namespace Game.Fight.AI.Evaluation
{
    public sealed class HealEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Allies == null || context.SpellBook?.HealSpells == null)
                yield break;

            foreach (var spell in context.SpellBook.HealSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                var estimatedHeal = SpellEvaluator.EstimateHeal(spell);
                foreach (var ally in context.Allies)
                {
                    if (ally?.Cell == null || ally.IsFighterDead || ally.MaxLife <= 0)
                        continue;

                    var missingLife = ally.MaxLife - ally.Life;
                    if (missingLife <= 0)
                        continue;

                    if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, ally.Cell.Id))
                        continue;

                    var hpRatio = (double)ally.Life / ally.MaxLife;
                    var priority = hpRatio <= 0.20 ? AIDecisionPriority.Critical : AIDecisionPriority.High;
                    var score = missingLife + estimatedHeal + (int)((1.0 - hpRatio) * 500);
                    if (ally == context.Fighter)
                        score += 80;

                    yield return new AIDecision
                    {
                        Type = AIDecisionType.Heal,
                        Priority = priority,
                        Score = score,
                        SpellId = spell.SpellId,
                        TargetId = ally.Id,
                        CellId = (short)ally.Cell.Id,
                        Reason = ally == context.Fighter ? "Self heal" : "Heal wounded ally"
                    };
                }
            }
        }
    }
}
