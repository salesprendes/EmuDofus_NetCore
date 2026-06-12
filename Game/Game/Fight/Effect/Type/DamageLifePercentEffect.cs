using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class DamageLifePercentEffect : AbstractSpellEffect
    {
        private EffectEnum m_damageType;

        public DamageLifePercentEffect(EffectEnum damageType)
        {
            m_damageType = damageType;
        }

        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            if (castInfos.Target != castInfos.Caster)
                return FightActionResultEnum.RESULT_NOTHING;

            var damageInfos = new CastInfos(m_damageType, -1, -1, -1, -1, -1, -1, -1, castInfos.Caster, castInfos.Target);
            var damageJet = (castInfos.Target.Life / 100) * castInfos.RandomJet;

            return DamageEffect.ApplyDamages(damageInfos, damageInfos.Target, ref damageJet);
        }
    }
}


