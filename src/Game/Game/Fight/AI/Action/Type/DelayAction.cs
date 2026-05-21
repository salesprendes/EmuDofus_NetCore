using System;

namespace Game.Fight.AI.Action.Type
{
    public sealed class DelayAction : AIAction
    {
        private readonly int m_delay;

        public DelayAction(AIFighter fighter, int delay) : base(fighter)
        {
            m_delay = Math.Max(0, delay);
        }

        public override AIActionResult Initialize()
        {
            if (m_delay == 0)
                return AIActionResult.SUCCESS;

            Timeout = m_delay;
            return AIActionResult.RUNNING;
        }

        public override AIActionResult Execute()
        {
            return Timedout ? AIActionResult.SUCCESS : AIActionResult.RUNNING;
        }
    }
}


