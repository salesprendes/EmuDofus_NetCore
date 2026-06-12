using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class StateBuff : AbstractSpellBuff
    {
        public StateBuff(CastInfos CastInfos, AbstractFighter Target)
    : base(CastInfos, Target, ActiveType.ACTIVE_STATS, DecrementType.TYPE_ENDTURN)
        {
            var damageValue = 0;

            ApplyEffect(ref damageValue);
        }

        public override FightActionResultEnum ApplyEffect(ref int DamageValue, CastInfos DamageInfos = null)
        {
            Target.StateManager.AddState(this);

            return base.ApplyEffect(ref DamageValue, DamageInfos);
        }

        public override FightActionResultEnum RemoveEffect()
        {
            Target.StateManager.RemoveState(this);

            return base.RemoveEffect();
        }
    }
}


