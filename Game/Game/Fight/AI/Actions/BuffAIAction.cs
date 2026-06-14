using Game.Fight.AI.Core;
using Game.Fight;
using Game.Spell;
using System.Linq;

namespace Game.Fight.AI.Actions
{
    public sealed class BuffAIAction : SpellAIActionBase
    {
        public override AIDecisionType Type => AIDecisionType.Buff;

        public BuffAIAction(AIDecision decision)
            : base(decision)
        {
        }

        public override bool CanExecute(AIContext context)
        {
            if (!base.CanExecute(context) || Decision.TargetId == null)
                return false;

            var target = context.Allies?.FirstOrDefault(a => a.Id == Decision.TargetId.Value);
            if (target == null || target.IsFighterDead)
                return false;

            var spell = GetSpell(context);




            if (spell != null && WouldRecastActiveState(spell, target))
                return false;





            if (Decision.SpellId.HasValue && HasActiveBuffFromSpell(target, Decision.SpellId.Value))
                return false;

            return true;
        }



        private static bool WouldRecastActiveState(SpellLevel spell, AbstractFighter target)
        {
            if (spell?.Effects == null || target?.StateManager == null)
                return false;

            foreach (var effect in spell.Effects)
            {
                if (effect == null)
                    continue;

                if (effect.TypeEnum == EffectEnum.ESTADO_MAS && effect.Value3 > 0
                    && target.StateManager.HasState((FighterStateEnum)effect.Value3))
                    return true;
            }

            return false;
        }

        private static bool HasActiveBuffFromSpell(AbstractFighter target, int spellId)
        {
            if (target?.BuffManager == null)
                return false;

            foreach (var buff in target.BuffManager.GetAllBuffs())
            {
                if (buff?.CastInfos != null && buff.CastInfos.SpellId == spellId)
                    return true;
            }

            return false;
        }
    }
}
