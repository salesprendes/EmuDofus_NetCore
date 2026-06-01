using Game.Fight.AI.Core;
using Game.Spell;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    public sealed class SummonEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Fighter == null || context.SpellBook?.SummonSpells == null)
                yield break;

            var maxInvocations = context.Fighter.Statistics.GetTotal(EffectEnum.AddInvocationMax);
            var currentInvocations = context.Allies?.Count(f => f.Invocator == context.Fighter && !f.StaticInvocation) ?? 0;
            if (currentInvocations >= maxInvocations)
                yield break;

            var movement = new MovementEvaluator();
            foreach (var spell in context.SpellBook.SummonSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                var cellId = movement.GetBestSummonCell(context, spell);
                if (!cellId.HasValue)
                    continue;

                yield return new AIDecision
                {
                    Type = AIDecisionType.Summon,
                    Priority = AIDecisionPriority.High,
                    Score = 180 + (maxInvocations - currentInvocations) * 20,
                    SpellId = spell.SpellId,
                    CellId = (short)cellId.Value,
                    Reason = "Useful summon cell"
                };
            }
        }
    }
}
