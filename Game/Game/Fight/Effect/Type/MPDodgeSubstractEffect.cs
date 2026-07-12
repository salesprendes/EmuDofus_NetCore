using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class MPDodgeSubstractEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            var damageValue = 0;

            if (castInfos.Duration > 1)
            {
                var subInfos = new CastInfos(EffectEnum.STAT_MENOS_PM_ESQUIVABLE, castInfos.SpellId, 0, castInfos.Value1, 0, 0, 0, castInfos.Duration, castInfos.Caster, null);
                var buff = new MPDodgeSubstractBuff(subInfos, castInfos.Target);

                // Igual que la variante de PA: sin este ApplyEffect la retirada de PM con
                // duración >1 no quitaba PM nunca (el buff quedaba inerte en la lista).
                buff.ApplyEffect(ref damageValue);
                castInfos.Target.BuffManager.AddBuff(buff);
            }
            else
            {
                var subInfos = new CastInfos(EffectEnum.STAT_MENOS_PM_ESQUIVABLE, castInfos.SpellId, 0, castInfos.Value1, 0, 0, 0, 0, castInfos.Caster, null);
                var buff = new MPDodgeSubstractBuff(subInfos, castInfos.Target);

                buff.ApplyEffect(ref damageValue);
                castInfos.Target.BuffManager.AddBuff(buff);
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


