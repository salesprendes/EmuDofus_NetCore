using System;

namespace Game.Fight.AI.Actions
{
    public sealed class DelayAIAction : AIActionBase
    {
        private readonly int m_delay;

        public DelayAIAction(AIFighter fighter, int delay)
    : base(fighter)
        {
            m_delay = Math.Max(0, delay);
        }

        protected override ChainResult OnInitialize()
        {
            if (m_delay == 0)
                return ChainResult.Done;

            Timeout = m_delay;
            return ChainResult.Running;
        }

        protected override ChainResult OnExecute()
        {
            return Timedout ? ChainResult.Done : ChainResult.Running;
        }
    }
}
