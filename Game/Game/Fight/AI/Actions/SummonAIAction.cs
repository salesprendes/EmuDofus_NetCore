using Game.Fight.AI.Core;
using Game.Spell;
using System.Linq;

namespace Game.Fight.AI.Actions
{
    public sealed class SummonAIAction : SpellAIActionBase
    {
        public override AIDecisionType Type => AIDecisionType.Summon;

        public SummonAIAction(AIDecision decision)
            : base(decision)
        {
        }

        public override bool CanExecute(AIContext context)
        {
            if (!base.CanExecute(context))
                return false;

            var spell = GetSpell(context);
            if (spell?.Effects == null || !spell.Effects.Any(e => e.TypeEnum == EffectEnum.Invocation
                    || e.TypeEnum == EffectEnum.InvocDouble
                    || e.TypeEnum == EffectEnum.InvocationStatic))
                return false;

            var maxInvocations = context.Fighter.Statistics.GetTotal(EffectEnum.AddInvocationMax);
            var currentInvocations = context.Allies?.Count(f => f.Invocator == context.Fighter && !f.StaticInvocation) ?? 0;
            return currentInvocations < maxInvocations;
        }
    }
}
