using Game.Action;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class MPDodgeSubstractBuff : AbstractSpellBuff
    {
        public MPDodgeSubstractBuff(CastInfos CastInfos, AbstractFighter Target)
    : base(CastInfos, Target, ActiveType.ACTIVE_STATS, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            var mpLost = CastInfos.Value1 > Target.MP ? Target.MP : CastInfos.Value1;
            CastInfos.Value1 = Target.CalculDodgeAPMP(CastInfos.Caster, mpLost, true);

            if (CastInfos.Value1 != mpLost)
            {
                Target.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_DODGE_SUBPM, Target.Id, Target.Id + "," + (mpLost - CastInfos.Value1)));
            }

            if (CastInfos.Value1 > 0)
            {
                var buff = new StatsBuff(new CastInfos(CastInfos.EffectType, CastInfos.SpellId, CastInfos.SpellId, CastInfos.Value1, 0, 0, 0, Duration, CastInfos.Caster, null), Target);
                buff.ApplyEffect(ref mpLost);
                Target.BuffManager.AddBuff(buff);
            }

            return base.ApplyEffect(ref damageValue, damageInfos);
        }

        public override FightActionResultEnum RemoveEffect()
        {
            return base.RemoveEffect();
        }
    }
}


