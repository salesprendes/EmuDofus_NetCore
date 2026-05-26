using Protocolo.Framework.Generic;
using Game.Entity;
using Game.Job;
using Game.Map;

namespace Game.Interactive.Type
{
    /// <summary>
    /// Door-like animated map object that temporarily makes its cell walkable.
    /// </summary>
    public sealed class AnimatedDoor : InteractiveObject
    {
        public const int FRAME_CLOSED = 1;
        public const int FRAME_OPENING = 2;
        public const int FRAME_OPENED = 3;
        public const int FRAME_CLOSING = 4;

        private enum DoorState
        {
            Closed,
            Opening,
            Opened,
            Closing,
        }

        private readonly int m_openingDuration;
        private readonly int m_openedDuration;
        private readonly int m_closingDuration;
        private readonly bool m_interactiveWhenIdle;

        private UpdatableTimer m_readyTimer;
        private UpdatableTimer m_closeTimer;
        private UpdatableTimer m_resetTimer;

        private DoorState m_state;

        public int OpeningDuration => m_openingDuration;

        public bool IsOpened => m_state == DoorState.Opened;

        public bool IsClosed => m_state == DoorState.Closed;

        public AnimatedDoor(MapInstance map, int cellId, int openingDuration, int openedDuration, int closingDuration, bool interactiveWhenIdle = false) : base(map, cellId, true)
        {
            m_openingDuration = openingDuration;
            m_openedDuration = openedDuration;
            m_closingDuration = closingDuration;
            m_interactiveWhenIdle = interactiveWhenIdle;
            m_state = DoorState.Closed;

            m_frameId = FRAME_CLOSED;
            IsActive = interactiveWhenIdle;
        }

        public int OpenTemporarily(int openedDuration = -1)
        {
            if (m_state == DoorState.Opened)
                return 0;

            if (m_state == DoorState.Opening)
                return m_openingDuration;

            ClearTimers();
            SetState(DoorState.Opening);

            m_readyTimer = AddTimer(m_openingDuration, () =>
            {
                SetState(DoorState.Opened);

                var duration = openedDuration >= 0 ? openedDuration : m_openedDuration;
                if (duration > 0)
                    m_closeTimer = AddTimer(duration, Close, true);
            }, true);

            return m_openingDuration;
        }

        public void Close()
        {
            if (m_state == DoorState.Closed || m_state == DoorState.Closing)
                return;

            ClearTimers();
            SetState(DoorState.Closing);
            m_resetTimer = AddTimer(m_closingDuration, () => SetState(DoorState.Closed), true);
        }

        public void ForceOpen()
        {
            ClearTimers();
            SetState(DoorState.Opened);
        }

        public void ForceClose()
        {
            ClearTimers();
            Map.SendAnimatedDoorCellState(CellId, false);
            SetState(DoorState.Closed);
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (skill == null)
                return;

            switch (skill.Id)
            {
                case SkillIdEnum.SKILL_ENTRER:
                case SkillIdEnum.SKILL_OUVRIR:
                case SkillIdEnum.SKILL_SORTIR:
                case SkillIdEnum.SKILL_UTILISER:
                    OpenTemporarily();
                    break;

                default:
                    base.UseWithSkill(character, skill);
                    break;
            }
        }

        private void SetState(DoorState state)
        {
            m_state = state;

            switch (state)
            {
                case DoorState.Closed:
                    m_frameId = FRAME_CLOSED;
                    IsActive = m_interactiveWhenIdle;
                    break;

                case DoorState.Opening:
                    m_frameId = FRAME_OPENING;
                    IsActive = false;
                    break;

                case DoorState.Opened:
                    m_frameId = FRAME_OPENED;
                    IsActive = m_interactiveWhenIdle;
                    Map.SendAnimatedDoorCellState(CellId, true);
                    break;

                case DoorState.Closing:
                    m_frameId = FRAME_CLOSING;
                    IsActive = false;
                    Map.SendAnimatedDoorCellState(CellId, false);
                    break;
            }

            SendUpdate();
        }

        private void ClearTimers()
        {
            RemoveTimer(ref m_readyTimer);
            RemoveTimer(ref m_closeTimer);
            RemoveTimer(ref m_resetTimer);
        }

        private void RemoveTimer(ref UpdatableTimer timer)
        {
            if (timer == null)
                return;

            RemoveTimer(timer);
            timer = null;
        }
    }
}
