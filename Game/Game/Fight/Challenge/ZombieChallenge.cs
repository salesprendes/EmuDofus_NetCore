using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class ZombieChallenge : AbstractChallenge
    {
        private bool m_hasMoved = false;

        public ZombieChallenge()
    : base(ChallengeTypeEnum.ZOMBIE)
        {
            BasicDropBonus = 30;
            BasicXpBonus = 30;

            TeamDropBonus = 50;
            TeamXpBonus = 50;

        }

        public override void CheckMovement(AbstractFighter fighter, int beginCell, int endCell, int length)
        {
            if (length != 1 || m_hasMoved)
                base.OnFailed(fighter.Name);
            else
                m_hasMoved = true;
        }

        public override void EndTurn(AbstractFighter fighter)
        {
            if (!m_hasMoved)
                OnFailed(fighter.Name);
            m_hasMoved = false;
        }
    }
}


