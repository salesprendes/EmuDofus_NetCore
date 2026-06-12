using Game.Fight;

namespace Game.Action
{
    public abstract class AbstractGameFightAction : AbstractGameAction
    {
        public override bool CanAbort => false;

        public long Timeout
        {
            get;
            private set;
        }

        public AbstractFighter Fighter
        {
            get;
            private set;
        }

        public AbstractGameFightAction(GameActionTypeEnum type, AbstractFighter fighter, long duration) : base(type, fighter, duration)
        {
            Fighter = fighter;
            Timeout = Fighter.Fight.UpdateTime + duration;
        }
    }
}


