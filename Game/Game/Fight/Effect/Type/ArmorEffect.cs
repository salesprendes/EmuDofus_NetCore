using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class ArmorEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;


            switch (castInfos.SpellId)
            {
                case 1:
                    castInfos.Target.Statistics.AddDon(EffectEnum.STAT_MAS_ARMADURA_FUEGO, castInfos.Value1);
                    break;

                case 6:
                    castInfos.Target.Statistics.AddDon(EffectEnum.STAT_MAS_ARMADURA_TIERRA, castInfos.Value1);
                    break;

                case 14:
                    castInfos.Target.Statistics.AddDon(EffectEnum.STAT_MAS_ARMADURA_AIRE, castInfos.Value1);
                    break;

                case 18:
                    castInfos.Target.Statistics.AddDon(EffectEnum.STAT_MAS_ARMADURA_AGUA, castInfos.Value1);
                    break;

                default:
                    castInfos.Target.Statistics.AddDon(EffectEnum.STAT_MAS_ARMADURA, castInfos.Value1);
                    break;
            }


            castInfos.Target.BuffManager.AddBuff(new ArmorBuff(castInfos, castInfos.Target));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


