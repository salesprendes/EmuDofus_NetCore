using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class PandaCarrierEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            var carrierInfos = new CastInfos(castInfos.EffectType, castInfos.SpellId, 0, 0, 0, (int)FighterStateEnum.STATE_CARRIER, 0, int.MaxValue - 1, castInfos.Caster, null);
            var carriedInfos = new CastInfos(castInfos.EffectType, castInfos.SpellId, 0, 0, 0, (int)FighterStateEnum.STATE_CARRIED, 0, int.MaxValue - 1, castInfos.Caster, null);

            castInfos.Caster.BuffManager.AddBuff(new PandaCarrierBuff(carrierInfos, castInfos.Target));
            castInfos.Target.BuffManager.AddBuff(new PandaCarriedBuff(carriedInfos, castInfos.Target));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


