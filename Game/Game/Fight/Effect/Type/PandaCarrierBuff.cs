using Game.Spell;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class PandaCarrierBuff : AbstractSpellBuff
    {
        public PandaCarrierBuff(CastInfos castInfos, AbstractFighter target)
    : base(castInfos, target, ActiveType.ACTIVE_ENDMOVE, DecrementType.TYPE_ENDMOVE)
        {
            Caster.StateManager.AddState(this);

            castInfos.Caster.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.PandaCarrier, castInfos.Caster.Id, target.Id.ToString()));
        }

        public override FightActionResultEnum ApplyEffect(ref int DamageValue, CastInfos DamageInfos = null)
        {

            if (!Target.StateManager.HasState(FighterStateEnum.STATE_CARRIED))
            {
                Duration = 0;
                return FightActionResultEnum.RESULT_NOTHING;
            }


            return Target.SetCell(Caster.Cell);
        }

        public override FightActionResultEnum RemoveEffect()
        {
            Caster.StateManager.RemoveState(this);

            return base.RemoveEffect();
        }
    }
}


