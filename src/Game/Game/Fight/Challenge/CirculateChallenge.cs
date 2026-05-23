using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CirculateChallenge : AbstractChallenge
    {
        /// <summary>
        /// 
        /// </summary>
        public CirculateChallenge()
            : base(ChallengeTypeEnum.CIRCULATE)
        {
            BasicDropBonus = 20;
            BasicXpBonus = 20;

            TeamDropBonus = 20;
            TeamXpBonus = 20;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fighter"></param>
        /// <param name="castInfos"></param>
        public override void CheckSpell(AbstractFighter fighter, Effect.CastInfos castInfos)
        {
            if((castInfos.EffectType == Spell.EffectEnum.SubMP ||
                castInfos.EffectType == Spell.EffectEnum.MPSteal) &&
                castInfos.Target != null &&
                castInfos.Target.Team != fighter.Team)            
                base.OnFailed(fighter.Name);            
        }
    }
}


