using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public sealed class PetulantChallenge : AbstractChallenge
    {
        public PetulantChallenge()
     : base(ChallengeTypeEnum.PETULANT)
        {
            BasicDropBonus = 10;
            BasicXpBonus = 10;

            TeamDropBonus = 10;
            TeamXpBonus = 10;

        }

        public override void EndTurn(AbstractFighter fighter)
        {
            if (fighter.AP > 0)
                base.OnFailed(fighter.Name);
        }
    }
}


