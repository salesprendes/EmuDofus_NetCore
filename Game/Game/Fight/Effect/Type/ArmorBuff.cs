using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class ArmorBuff : AbstractSpellBuff
    {
        public ArmorBuff(CastInfos castInfos, AbstractFighter target)
    : base(castInfos, target, ActiveType.ACTIVE_ATTACKED_AFTER_JET, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum RemoveEffect()
        {

            switch (CastInfos.SpellId)
            {
                case 1:
                    Target.Statistics.GetEffect(EffectEnum.AddArmorFire).Dons -= CastInfos.Value1;
                    break;

                case 6:
                    Target.Statistics.GetEffect(EffectEnum.AddArmorEarth).Dons -= CastInfos.Value1;
                    break;

                case 14:
                    Target.Statistics.GetEffect(EffectEnum.AddArmorAir).Dons -= CastInfos.Value1;
                    break;

                case 18:
                    Target.Statistics.GetEffect(EffectEnum.AddArmorWater).Dons -= CastInfos.Value1;
                    break;

                default:
                    Target.Statistics.GetEffect(EffectEnum.AddArmor).Dons -= CastInfos.Value1;
                    break;
            }

            return base.RemoveEffect();
        }
    }
}


