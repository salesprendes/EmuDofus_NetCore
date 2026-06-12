using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class EcaflipChanceBuff : AbstractSpellBuff
    {
        public EcaflipChanceBuff(CastInfos castInfos, AbstractFighter target)
    : base(castInfos, target, ActiveType.ACTIVE_ATTACKED_AFTER_JET, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            var damageCoef = CastInfos.Value1;
            var healCoef = CastInfos.Value2;
            var chance = CastInfos.Value3;
            var chanceJet = Util.Next(0, 100);

            if (chanceJet < chance)
            {
                var HealValue = damageValue * healCoef;

                if (HealEffect.ApplyHeal(CastInfos, Target, ref HealValue) == FightActionResultEnum.RESULT_END)
                    return FightActionResultEnum.RESULT_END;

                damageValue = 0;
            }
            else
            {
                damageValue *= damageCoef;
            }

            return base.ApplyEffect(ref damageValue, damageInfos);
        }
    }
}


