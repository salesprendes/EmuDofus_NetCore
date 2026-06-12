using Game.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class AnachoriteChallenge : AbstractChallenge
    {
        public AnachoriteChallenge()
    : base(ChallengeTypeEnum.ANACHORITE)
        {
            BasicDropBonus = 20;
            BasicXpBonus = 20;

            TeamDropBonus = 30;
            TeamXpBonus = 30;

        }

        public override void EndTurn(AbstractFighter fighter)
        {
            var aroundFighters = Pathfinding.GetFightersNear(fighter.Fight, fighter.Cell.Id);
            if (aroundFighters.Where(f => f.Team == fighter.Team).Count() > 0)
                base.OnFailed(fighter.Name);
        }
    }
}


