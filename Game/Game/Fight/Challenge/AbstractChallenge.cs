using Game.Database.Structure;
using Game.Fight.Effect;
using Game.Network;

namespace Game.Fight.Challenge
{
    public abstract class AbstractChallenge : MessageDispatcher
    {
        public int Id
        {
            get;
            private set;
        }

        public bool Success
        {
            get;
            protected set;
        }

        public bool Failed
        {
            get;
            protected set;
        }

        public bool ShowTarget
        {
            get;
            protected set;
        }

        public long TargetId
        {
            get;
            protected set;
        }

        public long BasicXpBonus
        {
            get;
            protected set;
        }

        public long TeamXpBonus
        {
            get;
            protected set;
        }

        public long BasicDropBonus
        {
            get;
            protected set;
        }

        public long TeamDropBonus
        {
            get;
            protected set;
        }

        public AbstractFighter Target
        {
            get;
            protected set;
        }

        public AbstractChallenge(ChallengeTypeEnum type)
        {
            Id = (int)type;
            Success = false;
        }

        public virtual void StartFight(FightTeam team)
        {

        }

        public virtual void BeginTurn(AbstractFighter fighter)
        {

        }

        public virtual void EndTurn(AbstractFighter fighter)
        {

        }

        public virtual void CheckSpell(AbstractFighter fighter, CastInfos castInfos)
        {
        }

        public virtual void CheckMovement(AbstractFighter fighter, int beginCell, int endCell, int length)
        {
        }

        public virtual void CheckWeapon(AbstractFighter fighter, ItemTemplateDAO weaponTemplate)
        {

        }

        public virtual void CheckDeath(AbstractFighter fighter)
        {

        }

        public virtual void OnSuccess()
        {
            if (!Success && !Failed)
            {
                Success = true;
                Failed = false;
                base.Dispatch(WorldMessage.FIGHT_CHALLENGE_SUCCESS(Id));
            }
        }

        public virtual void OnFailed(string name = "")
        {
            if (!Success && !Failed)
            {
                Success = false;
                Failed = true;
                base.CachedBuffer = true;
                base.Dispatch(WorldMessage.FIGHT_CHALLENGE_FAILED(Id));
                if (name != "")
                {
                    base.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_FIGHT_CHALLENGE_FAILED_DUE_TO, name));
                }

                base.CachedBuffer = false;
            }
        }

        public void FlagCell(int cellId, long fighterId = 0)
        {
            base.Dispatch(WorldMessage.FIGHT_CELL_FLAG(cellId, fighterId));
        }
    }
}


