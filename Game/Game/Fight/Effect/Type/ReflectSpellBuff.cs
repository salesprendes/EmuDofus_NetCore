using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class ReflectSpellBuff : AbstractSpellBuff
    {
        public ReflectSpellBuff(CastInfos castInfos, AbstractFighter target)
            : base(castInfos, target, ActiveType.ACTIVE_ATTACKED_AFTER_JET, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {

            if (damageInfos.Caster == Target)
                return FightActionResultEnum.RESULT_NOTHING;


            if (damageInfos.SpellId < 1)
                return FightActionResultEnum.RESULT_NOTHING;


            if (damageInfos.SpellLevel > CastInfos.Value2)
                return FightActionResultEnum.RESULT_NOTHING;


            if (damageInfos.IsReflect || damageInfos.IsPoison || damageInfos.IsTrap)
                return FightActionResultEnum.RESULT_NOTHING;


            damageValue = 0;


            damageInfos.IsReflect = true;


            var damageJet = damageInfos.RandomJet;


            return DamageEffect.ApplyDamages(damageInfos, damageInfos.Caster, ref damageJet);
        }
    }
}


