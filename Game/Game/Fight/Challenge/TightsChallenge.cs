using Game.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class TightsChallenge : AbstractChallenge
    {
        public TightsChallenge()
    : base(ChallengeTypeEnum.TIGHTS)
        {
            BasicDropBonus = 40;
            BasicXpBonus = 40;

            TeamDropBonus = 40;
            TeamXpBonus = 40;

        }

        public override void EndTurn(AbstractFighter fighter)
        {
            var nearestFighters = Pathfinding.GetFightersNear(fighter.Fight, fighter.Cell.Id);
            if (!nearestFighters.Any(f => f.Team == fighter.Team))
                base.OnFailed(fighter.Name);
        }
    }
}


