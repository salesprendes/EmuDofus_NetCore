using Game.Entity;
using System;

namespace Game.Action
{
    /// <summary>
    ///
    /// </summary>
    public abstract class AbstractGameAction
    {
        /// <summary>
        ///
        /// </summary>
        public long Duration
        {
            get;
            protected set;
        }

        public long StartedAt { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public GameActionTypeEnum Type
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract bool CanAbort
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        public AbstractEntity Entity
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsFinished { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entity"></param>
        /// <param name="duration"></param>
        protected AbstractGameAction(GameActionTypeEnum type, AbstractEntity entity, long duration = -1)
        {
            Type = type;
            Entity = entity;
            Duration = duration;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual void Start()
        {
            StartedAt = Environment.TickCount64;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public virtual void Abort(params object[] args)
        {
            IsFinished = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public virtual void Stop(params object[] args)
        {
            IsFinished = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual string SerializeAs_GameAction()
        {
            return "";
        }
    }
}


