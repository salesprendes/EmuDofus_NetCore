using Game.Fight.AI.Core;
using Game.Spell;
using System.Linq;

namespace Game.Fight.AI.Actions
{
    public sealed class HealAIAction : SpellAIActionBase
    {
        public override AIDecisionType Type => AIDecisionType.Heal;

        public HealAIAction(AIDecision decision)
            : base(decision)
        {
        }

        public override bool CanExecute(AIContext context)
        {
            if (!base.CanExecute(context) || Decision.TargetId == null)
                return false;

            var target = context.Allies?.FirstOrDefault(a => a.Id == Decision.TargetId.Value);
            if (target == null || target.IsFighterDead || target.Life >= target.MaxLife)
                return false;

            var spell = GetSpell(context);
            return spell?.Effects != null && spell.Effects.Any(e => e.TypeEnum == EffectEnum.Heal);
        }
    }
}
