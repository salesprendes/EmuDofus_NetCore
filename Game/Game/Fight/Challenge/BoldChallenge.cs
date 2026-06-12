using Game.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class BoldChallenge : AbstractChallenge
    {
        public BoldChallenge()
    : base(ChallengeTypeEnum.BOLD)
        {
            BasicDropBonus = 25;
            BasicXpBonus = 25;

            TeamDropBonus = 25;
            TeamXpBonus = 25;

        }

        public override void EndTurn(AbstractFighter fighter)
        {
            var nearestEnnemis = Pathfinding.GetEnnemiesNear(fighter.Fight, fighter.Team, fighter.Cell.Id);
            if (!nearestEnnemis.Any())
                base.OnFailed(fighter.Name);
        }
    }
}


