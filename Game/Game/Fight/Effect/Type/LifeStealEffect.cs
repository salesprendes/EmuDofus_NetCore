using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class LifeStealEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            var damageJet = castInfos.RandomJet;

            if (DamageEffect.ApplyDamages(castInfos, castInfos.Target, ref damageJet) == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            castInfos.EffectType = Spell.EffectEnum.DamageBrut;


            var healJet = damageJet / 2;

            return HealEffect.ApplyHeal(castInfos, castInfos.Caster, ref healJet);
        }
    }
}


