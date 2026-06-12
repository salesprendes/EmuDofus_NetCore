using Protocolo.Framework.Generic.Logging;
using System;

namespace Game.Fight.AI.Actions
{
    public abstract class AIActionBase
    {
        protected static readonly ILogger Logger = LogManager.GetLogger(typeof(AIActionBase));


        private enum Phase { Init, Exec, Finish }
        private Phase m_phase = Phase.Init;
        private bool m_finishCalled;


        public AIFighter Fighter { get; }
        public AIActionBase NextAction { get; private set; }

        public bool IsFinished => m_phase == Phase.Finish && m_finishCalled;


        private long m_timeoutTick;

        protected long Timeout
        {
            set => m_timeoutTick = (Fighter?.Fight?.UpdateTime ?? 0) + Math.Max(0, value);
        }

        protected bool Timedout => Fighter?.Fight != null && Fighter.Fight.UpdateTime >= m_timeoutTick;


        protected AIActionBase(AIFighter fighter)
        {
            Fighter = fighter;
        }


        public AIActionBase LinkWith(AIActionBase next)
        {
            NextAction = next;
            return next;
        }


        public void Update()
        {
            switch (m_phase)
            {
                case Phase.Init:
                    m_phase = OnInitialize() == ChainResult.Running ? Phase.Exec : Phase.Finish;
                    break;

                case Phase.Exec:
                    if (OnExecute() != ChainResult.Running)
                        m_phase = Phase.Finish;
                    break;

                case Phase.Finish:
                    if (!m_finishCalled)
                    {
                        OnFinish();
                        m_finishCalled = true;
                    }
                    break;
            }
        }



        protected virtual ChainResult OnInitialize() => ChainResult.Running;

        protected abstract ChainResult OnExecute();

        protected virtual void OnFinish() { }


        protected enum ChainResult
        {
            Running,

            Done
        }
    }
}
