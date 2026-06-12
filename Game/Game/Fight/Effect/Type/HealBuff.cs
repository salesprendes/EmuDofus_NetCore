using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class HealBuff : AbstractSpellBuff
    {
        public HealBuff(CastInfos castInfos, AbstractFighter target)
    : base(castInfos, target, ActiveType.ACTIVE_BEGINTURN, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int healValue, CastInfos healInfos = null)
        {
            var heal = CastInfos.RandomJet;

            return HealEffect.ApplyHeal(CastInfos, Target, ref heal);
        }
    }
}


