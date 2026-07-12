namespace Game.Fight.Challenge
{
    public sealed class StatueChallenge : AbstractChallenge
    {
        private int m_cellId;

        public StatueChallenge() : base(ChallengeTypeEnum.STATUE)
        {
            BasicDropBonus = 25;
            BasicXpBonus = 25;

            TeamDropBonus = 55;
            TeamXpBonus = 55;
        }

        public override void BeginTurn(AbstractFighter fighter)
        {
            m_cellId = fighter.Cell.Id;
        }

        public override void EndTurn(AbstractFighter fighter)
        {
            if (fighter.Cell.Id != m_cellId)
                base.OnFailed(fighter.Name);
        }
    }
}


