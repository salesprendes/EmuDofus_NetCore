using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class IncreaseSpellJetEffect : AbstractSpellEffect
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="castInfos"></param>
        /// <returns></returns>
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            var target = castInfos.Target ?? castInfos.Caster;
            if (target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            castInfos.Caster.BuffManager.AddBuff(new IncreaseSpellJetBuff(castInfos, castInfos.Caster));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


