using Game.Spell;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class BuffRemoveEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos CastInfos)
        {
            if (CastInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            if (CastInfos.Target.BuffManager.Debuff() == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            CastInfos.Target.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.DeleteAllBonus, CastInfos.Target.Id, CastInfos.Target.Id.ToString()));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


