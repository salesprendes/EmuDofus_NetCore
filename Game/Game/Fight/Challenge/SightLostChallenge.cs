using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class SightLostChallenge : AbstractChallenge
    {
        public SightLostChallenge()
    : base(ChallengeTypeEnum.LOST_SIGHT)
        {
            BasicDropBonus = 15;
            BasicXpBonus = 15;

            TeamDropBonus = 15;
            TeamXpBonus = 15;

        }

        public override void CheckSpell(AbstractFighter fighter, Effect.CastInfos castInfos)
        {
            if ((castInfos.EffectType == Spell.EffectEnum.STAT_MENOS_ALCANCE ||
                castInfos.EffectType == Spell.EffectEnum.STAT_ROBO_ALCANCE) &&
                castInfos.Target != null &&
                castInfos.Target.Team != fighter.Team)
                base.OnFailed(fighter.Name);
        }
    }
}


