using Game.Fight.AI.Core;
using Game.Spell;
using Protocolo.Framework.Generic.Logging;
using System.Linq;

namespace Game.Fight.AI.Actions
{
    public sealed class SummonAIAction : SpellAIActionBase
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(SummonAIAction));

        public override AIDecisionType Type => AIDecisionType.Summon;

        public SummonAIAction(AIDecision decision) : base(decision) { }

        public override bool CanExecute(AIContext context)
        {
            if (!base.CanExecute(context))
            {
                Logger.Warn($"[SummonAIAction] base.CanExecute fallo para luchador={(context?.Fighter?.Id ?? 0)} hechizoId={Decision?.SpellId}");
                return false;
            }

            var spell = GetSpell(context);
            if (spell?.Effects == null || !spell.Effects.Any(e => e.TypeEnum == EffectEnum.Invocation || e.TypeEnum == EffectEnum.InvocDouble || e.TypeEnum == EffectEnum.InvocationStatic))
            {
                Logger.Warn($"[SummonAIAction] El hechizo {Decision?.SpellId} no tiene efecto de invocacion. Efectos: {(spell?.Effects == null ? "null" : string.Join(",", spell.Effects.Select(e => (int)e.TypeEnum)))}");
                return false;
            }

            var maxInvocations = context.Fighter.Statistics.GetTotal(EffectEnum.AddInvocationMax);
            var currentInvocations = context.Allies?.Count(f => f.Invocator == context.Fighter && !f.StaticInvocation) ?? 0;
            if (currentInvocations >= maxInvocations)
            {
                Logger.Warn($"[SummonAIAction] Limite de invocaciones: actual={currentInvocations} maximo={maxInvocations} luchador={(context?.Fighter?.Id ?? 0)}");
                return false;
            }

            return true;
        }
    }
}
