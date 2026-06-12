using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class PureLifeStealEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            var damageJet = castInfos.RandomJet;
            castInfos.EffectType = EffectEnum.DamageBrut;


            if (castInfos.Caster.Team == castInfos.Target.Team && damageJet > castInfos.Target.Life)
                damageJet = castInfos.Target.Life - 1;

            if (DamageEffect.ApplyDamages(castInfos, castInfos.Target, ref damageJet) == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            var healJet = damageJet / 2;

            return HealEffect.ApplyHeal(castInfos, castInfos.Caster, ref healJet);
        }
    }
}


