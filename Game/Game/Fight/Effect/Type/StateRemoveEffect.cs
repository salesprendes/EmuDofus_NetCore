using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class StateRemoveEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos CastInfos)
        {
            if (CastInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            CastInfos.Target.BuffManager.RemoveState(CastInfos.Value3);

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


