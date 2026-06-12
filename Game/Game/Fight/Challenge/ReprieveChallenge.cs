using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class ReprieveChallenge : AbstractChallenge
    {
        public ReprieveChallenge()
    : base(ChallengeTypeEnum.REPRIEVE)
        {
            BasicDropBonus = 20;
            BasicXpBonus = 20;

            TeamDropBonus = 55;
            TeamXpBonus = 55;

            ShowTarget = true;
        }

        public override void StartFight(FightTeam team)
        {
            if (team.OpponentTeam.HasSomeoneAlive)
            {
                var aliveEnemies = team.OpponentTeam.AliveFighters.ToList();
                var target = aliveEnemies[Util.Next(0, aliveEnemies.Count)];

                Target = target;
                TargetId = target.Id;
                base.FlagCell(target.Cell.Id, TargetId);
            }
        }

        public override void CheckDeath(AbstractFighter fighter)
        {
            if (fighter.Id == TargetId)
            {
                if (!fighter.Team.AliveFighters.Any())
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
}


