using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class IncreaseSpellJetBuff : AbstractSpellBuff
    {
        public IncreaseSpellJetBuff(CastInfos castInfos, AbstractFighter target)
    : base(castInfos, target, ActiveType.ACTIVE_ATTACK_BEFORE_JET, DecrementType.TYPE_ENDTURN)
        {
            Duration += 2;
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            if (damageInfos.SpellId == CastInfos.SpellId)
                damageValue += CastInfos.Value3;

            return base.ApplyEffect(ref damageValue, damageInfos);
        }
    }
}


