using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class SurvivorChallenge : AbstractChallenge
    {
        public SurvivorChallenge()
    : base(ChallengeTypeEnum.SURVIVOR)
        {
            BasicDropBonus = 30;
            BasicXpBonus = 30;

            TeamDropBonus = 30;
            TeamXpBonus = 30;

        }

        public override void EndTurn(AbstractFighter fighter)
        {
            if (fighter.Team.AliveFighters.Count() != fighter.Team.Fighters.Count)
                base.OnFailed(fighter.Name);
        }
    }
}


