using Game.Fight.Effect;
using Game.Spell;

namespace Game.Fight.Challenge
{
    public sealed class AbnegationChallenge : AbstractChallenge
    {
        public AbnegationChallenge() : base(ChallengeTypeEnum.ABNEGATION)
        {
            BasicDropBonus = 10;
            BasicXpBonus = 10;

            TeamDropBonus = 25;
            TeamXpBonus = 25;

        }

        public override void CheckSpell(AbstractFighter fighter, CastInfos castInfos)
        {
            if (castInfos.EffectType == EffectEnum.AddLife && castInfos.Target != null && castInfos.Target.Team == fighter.Team)
            {
                base.OnFailed(fighter.Name);
            }
        }
    }
}


