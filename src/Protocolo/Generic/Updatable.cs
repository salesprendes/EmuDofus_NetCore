using log4net;
using System;
using System.Collections.Generic;

namespace Protocolo.Framework.Generic
{
    /// <summary>
    ///
    /// </summary>
    public abstract class Updatable : IDisposable
    {
        protected static ILog Logger = LogManager.GetLogger(typeof(Updatable));

        private LockFreeQueue<Action> m_messagesQueue;
        private List<Updatable> m_subUpdatableObjects;
        private List<UpdatableTimer> m_timerList;

        public long UpdateTime
        {
            get;
            private set;
        }

        public int MessageCount
        {
            get
            {
                int count = m_messagesQueue.Count;
                for (int i = 0; i < m_subUpdatableObjects.Count; i++)
                    count += m_subUpdatableObjects[i].MessageCount;
                return count;
            }
        }

        protected Updatable()
        {
            m_messagesQueue = new LockFreeQueue<Action>();
            m_subUpdatableObjects = new List<Updatable>();
            m_timerList = new List<UpdatableTimer>();
        }

        public virtual void Dispose()
        {
            for (int i = 0; i < m_subUpdatableObjects.Count; i++)
                m_subUpdatableObjects[i].Dispose();
            m_subUpdatableObjects.Clear();
            m_messagesQueue.Clear();
            m_timerList.Clear();
        }

        public void ClearMessages()
        {
            AddMessage(() => m_messagesQueue.Clear());
        }

        public void AddMessage(Action message)
        {
            m_messagesQueue.Enqueue(message);
        }

        public void AddUpdatable(Updatable updatable)
        {
            AddMessage(() => m_subUpdatableObjects.Add(updatable));
        }

        public void RemoveUpdatable(Updatable updatable)
        {
            AddMessage(() => m_subUpdatableObjects.Remove(updatable));
        }

        public void AddLinkedMessages(params System.Action[] messages)
        {
            AddMessage(() =>
            {
                messages[0]();
                if (messages.Length > 1)
                    AddLinkedMessages(1, messages);
            });
        }

        public void AddLinkedMessages(int index = 0, params System.Action[] messages)
        {
            AddMessage(() =>
            {
                messages[index]();
                if (messages.Length > ++index)
                    AddLinkedMessages(index, messages);
            });
        }

        public UpdatableTimer AddTimer(int delay, Action callback, bool oneshot = false)
        {
            var timer = new UpdatableTimer(delay, callback, oneshot);
            AddTimer(timer);
            return timer;
        }

        public void AddTimer(UpdatableTimer timer)
        {
            AddMessage(() =>
            {
                timer.LastActivated = UpdateTime;
                m_timerList.Add(timer);
            });
        }

        public void RemoveTimer(UpdatableTimer timer)
        {
            AddMessage(() => m_timerList.Remove(timer));
        }

        public virtual void Update(long updateDelta)
        {
            UpdateTime += updateDelta;

            // Plain for-loop avoids LINQ WhereListIterator allocation on every tick
            int timerCount = m_timerList.Count;
            for (int i = 0; i < timerCount; i++)
            {
                var timer = m_timerList[i];
                if ((UpdateTime - timer.LastActivated) >= timer.Delay)
                {
                    try
                    {
                        timer.Tick(UpdateTime);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TaskQueue[" + GetType().Name + "] timer update failed [" + timer.GetType().Name + "] : " + ex.ToString());
                    }
                    if (timer.OneShot)
                        RemoveTimer(timer);
                }
            }

            int subCount = m_subUpdatableObjects.Count;
            for (int i = 0; i < subCount; i++)
            {
                try
                {
                    m_subUpdatableObjects[i].Update(updateDelta);
                }
                catch (Exception ex)
                {
                    Logger.Error("UpdatableObject[" + GetType().Name + "] update failed : " + ex.ToString());
                }
            }

            Action msg;
            while (m_messagesQueue.TryDequeue(out msg))
            {
                try
                {
                    msg();
                }
                catch (Exception ex)
                {
                    Logger.Error("UpdatableObject[" + GetType().Name + "] message failed to process : " + ex.ToString());
                }
            }
        }
    }
}
