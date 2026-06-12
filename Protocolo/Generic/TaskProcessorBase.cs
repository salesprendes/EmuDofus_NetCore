using Protocolo.Framework.Generic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Protocolo.Framework.Generic
{
    public abstract class TaskProcessorBase
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(TaskProcessorBase));

        public int UpdateInterval
        {
            get;
            private set;
        }

        public long LastUpdate
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public bool IsRunning
        {
            get
            {
                return m_running;
            }
        }

        private Stopwatch m_queueTimer;
        private LockFreeQueue<Action> m_messageQueue;
        private List<Updatable> m_updatableObjects;
        private List<UpdatableTimer> m_timerList;
        private volatile bool m_running;

        public TaskProcessorBase(string name, int updateInterval = 10)
        {
            UpdateInterval = updateInterval;
            Name = name;

            m_running = false;
            m_messageQueue = new LockFreeQueue<Action>();
            m_updatableObjects = new List<Updatable>();
            m_timerList = new List<UpdatableTimer>();
            m_queueTimer = new Stopwatch();

            Start();
        }
        public void Start()
        {
            m_running = true;
            m_queueTimer.Start();

            Task.Delay(UpdateInterval).ContinueWith(_ => InternalUpdate(), TaskScheduler.Default);
        }
        public void Stop()
        {
            AddMessage(() => { m_running = false; m_queueTimer.Reset(); LastUpdate = 0; });
        }
        public void AddMessage(Action message)
        {
            m_messageQueue.Enqueue(message);
        }
        public void AddLinkedMessages(params System.Action[] messages)
        {
            AddMessage(() => { messages[0](); if (messages.Length > 1) AddLinkedMessages(1, messages); });
        }
        public void AddLinkedMessages(int index = 0, params System.Action[] messages)
        {
            AddMessage(() => { messages[index](); if (messages.Length > ++index) AddLinkedMessages(index, messages); });
        }
        public void AddUpdatable(Updatable updatable)
        {
            AddMessage(() => { m_updatableObjects.Add(updatable); });
        }
        public void RemoveUpdatable(Updatable updatable)
        {
            AddMessage(() => { m_updatableObjects.Remove(updatable); });
        }

        public UpdatableTimer AddTimer(int delay, Action callback, bool oneshot = false)
        {
            var timer = new UpdatableTimer(delay, callback, oneshot);
            AddTimer(timer);
            return timer;
        }

        public void AddTimer(UpdatableTimer timer)
        {
            AddMessage(() => { timer.LastActivated = LastUpdate; m_timerList.Add(timer); });
        }

        public void RemoveTimer(UpdatableTimer timer)
        {
            AddMessage(() => { m_timerList.Remove(timer); });
        }

        private void InternalUpdate()
        {
            var timeStart = m_queueTimer.ElapsedMilliseconds;
            var updateDelta = timeStart - LastUpdate;
            LastUpdate = timeStart;


            for (int i = m_timerList.Count - 1; i >= 0; i--)
            {
                var timer = m_timerList[i];
                if ((LastUpdate - timer.LastActivated) >= timer.Delay)
                {
                    try
                    {
                        timer.Tick(LastUpdate);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"TaskQueue[{Name}] fallo al actualizar el temporizador [{timer.GetType().Name}]: {ex}");
                    }
                    if (timer.OneShot)
                        m_timerList.RemoveAt(i);
                }
            }

            int updatableCount = m_updatableObjects.Count;
            for (int i = 0; i < updatableCount; i++)
            {
                try
                {
                    m_updatableObjects[i].Update(updateDelta);
                }
                catch (Exception ex)
                {
                    Logger.Error($"TaskQueue[{Name}] fallo al actualizar el objeto [{m_updatableObjects[i].GetType().Name}]: {ex}");
                }
            }

            Action msg = null;
            while (m_messageQueue.TryDequeue(out msg))
            {
                try
                {
                    msg();
                }
                catch (Exception ex)
                {
                    Logger.Error($"TaskQueue[{Name}] fallo al procesar un mensaje: {ex}");
                }
            }

            var nextDelay = Math.Max(0, (int)((timeStart + UpdateInterval) - m_queueTimer.ElapsedMilliseconds));

            if (m_running)
                Task.Delay(nextDelay).ContinueWith(_ => InternalUpdate(), TaskScheduler.Default);
        }
    }
}
