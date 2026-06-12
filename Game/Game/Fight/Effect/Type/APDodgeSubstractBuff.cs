using Game.Action;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class APDodgeSubstractBuff : AbstractSpellBuff
    {
        public APDodgeSubstractBuff(CastInfos CastInfos, AbstractFighter Target)
    : base(CastInfos, Target, ActiveType.ACTIVE_STATS, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            var apLost = CastInfos.Value1 > Target.AP ? Target.AP : CastInfos.Value1;
            CastInfos.Value1 = Target.CalculDodgeAPMP(CastInfos.Caster, apLost);

            if (CastInfos.Value1 != apLost)
            {
                Target.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_DODGE_SUBPA, Target.Id, Target.Id + "," + (apLost - CastInfos.Value1)));
            }

            if (CastInfos.Value1 > 0)
            {
                var buffStats = new StatsBuff(new CastInfos(CastInfos.EffectType, CastInfos.SpellId, CastInfos.SpellId, CastInfos.Value1, 0, 0, 0, Duration, CastInfos.Caster, null), Target);
                buffStats.ApplyEffect(ref apLost);
                Target.BuffManager.AddBuff(buffStats);
            }

            return base.ApplyEffect(ref damageValue, damageInfos);
        }

        public override FightActionResultEnum RemoveEffect()
        {
            return base.RemoveEffect();
        }
    }
}


