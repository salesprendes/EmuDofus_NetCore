using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class PandaCarriedBuff : AbstractSpellBuff
    {
        public PandaCarriedBuff(CastInfos castInfos, AbstractFighter target)
   : base(castInfos, target, ActiveType.ACTIVE_ENDMOVE, DecrementType.TYPE_ENDMOVE)
        {
            Target.StateManager.AddState(this);

            Target.SetCell(Caster.Cell);
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            if (Caster.Cell.Id != Target.Cell.Id)
            {
                Target.BuffManager.RemoveState((int)FighterStateEnum.STATE_CARRIED);
                Caster.BuffManager.RemoveState((int)FighterStateEnum.STATE_CARRIER);

                Duration = 0;
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public override FightActionResultEnum RemoveEffect()
        {
            Target.StateManager.RemoveState(this);

            return base.RemoveEffect();
        }
    }
}


