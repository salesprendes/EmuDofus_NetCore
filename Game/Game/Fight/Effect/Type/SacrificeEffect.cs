using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class SacrificeEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            castInfos.Target.BuffManager.AddBuff(new SacrificeBuff(castInfos, castInfos.Target));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


