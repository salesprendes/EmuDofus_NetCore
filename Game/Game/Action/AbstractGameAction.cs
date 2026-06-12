using Game.Entity;
using System;

namespace Game.Action
{
    public abstract class AbstractGameAction
    {
        public long Duration
        {
            get;
            protected set;
        }

        public long StartedAt { get; private set; }

        public GameActionTypeEnum Type
        {
            get;
            private set;
        }

        public abstract bool CanAbort
        {
            get;
        }

        public AbstractEntity Entity
        {
            get;
            private set;
        }

        public bool IsFinished { get; protected set; }

        protected AbstractGameAction(GameActionTypeEnum type, AbstractEntity entity, long duration = -1)
        {
            Type = type;
            Entity = entity;
            Duration = duration;
        }

        public virtual void Start()
        {
            StartedAt = Environment.TickCount64;
        }

        public virtual void Abort(params object[] args)
        {
            IsFinished = true;
        }

        public virtual void Stop(params object[] args)
        {
            IsFinished = true;
        }

        public virtual string SerializeAs_GameAction()
        {
            return "";
        }
    }
}


