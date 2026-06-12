using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class AppointedVoluntaryChallenge : AbstractChallenge
    {
        public AppointedVoluntaryChallenge()
    : base(ChallengeTypeEnum.APPOINTED_VOLUNTARY)
        {
            BasicDropBonus = 30;
            BasicXpBonus = 30;

            TeamDropBonus = 60;
            TeamXpBonus = 60;

            ShowTarget = true;
        }

        public override void StartFight(FightTeam team)
        {
            if (team.OpponentTeam.HasSomeoneAlive)
            {
                var randomIndex = Util.Next(0, team.OpponentTeam.AliveFighters.Count());
                var target = team.OpponentTeam.AliveFighters.ElementAt(randomIndex);

                TargetId = target.Id;
                base.FlagCell(target.Cell.Id, TargetId);
            }
        }

        public override void CheckDeath(AbstractFighter fighter)
        {
            if (fighter.Id == TargetId)
            {
                base.OnSuccess();
            }
            else
            {
                base.OnFailed();
            }
        }
    }
}


