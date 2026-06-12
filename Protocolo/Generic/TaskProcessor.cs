using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Protocolo.Framework.Generic
{
    public abstract class TaskProcessor<T> : Singleton<T> where T : class, new()
    {
        public int UpdateInterval { get; private set; }
        public long LastUpdate { get; private set; }
        public string Name { get; private set; }
        public bool IsRunning => m_running;

        private readonly Stopwatch m_queueTimer;
        private readonly BlockingCollection<Action> m_messageQueue;
        private readonly List<Updatable> m_updatableObjects;
        private readonly List<UpdatableTimer> m_timerList;
        private volatile bool m_running;
        private CancellationTokenSource m_cts;
        private Thread m_updateThread;

        protected TaskProcessor(string name, int updateInterval = 10)
        {
            UpdateInterval = updateInterval;
            Name = name;
            m_messageQueue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
            m_updatableObjects = new List<Updatable>();
            m_timerList = new List<UpdatableTimer>();
            m_queueTimer = new Stopwatch();
            Start();
        }

        public void Start()
        {
            if (m_running) return;
            m_cts = new CancellationTokenSource();
            m_running = true;
            m_queueTimer.Start();
            m_updateThread = new Thread(InternalStart) { IsBackground = true, Name = $"TaskProcessor-{Name}"};
            m_updateThread.Start();
        }

        public void Stop()
        {
            m_cts?.Cancel();
            AddMessage(() => { m_running = false; m_queueTimer.Reset(); LastUpdate = 0; Logger.Info($"TaskQueue[{Name}] detenida."); });
        }

        public void AddMessage(Action message)
        {
            if (m_running)
                m_messageQueue.Add(message);
        }

        public void AddLinkedMessages(params Action[] messages)
        {
            if (messages == null || messages.Length == 0) return;
            AddMessage(() => { messages[0](); if (messages.Length > 1) AddLinkedMessages(1, messages); });
        }

        public void AddLinkedMessages(int index, params Action[] messages)
        {
            AddMessage(() => { messages[index](); if (messages.Length > ++index) AddLinkedMessages(index, messages); });
        }

        public void AddUpdatable(Updatable updatable) =>
            AddMessage(() => m_updatableObjects.Add(updatable));

        public void RemoveUpdatable(Updatable updatable) =>
            AddMessage(() => m_updatableObjects.Remove(updatable));

        public UpdatableTimer AddTimer(int delay, Action callback, bool oneshot = false)
        {
            var timer = new UpdatableTimer(delay, callback, oneshot);
            AddTimer(timer);
            return timer;
        }

        public void AddTimer(UpdatableTimer timer) =>
            AddMessage(() =>
            {
                timer.LastActivated = LastUpdate;
                m_timerList.Add(timer);
            });

        public void RemoveTimer(UpdatableTimer timer) =>
            AddMessage(() => m_timerList.Remove(timer));

        private void InternalStart()
        {
            Logger.Info($"TaskQueue[{Name}] iniciada.");
            while (m_running)
                InternalUpdate();
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
                    try { timer.Tick(LastUpdate); }
                    catch (Exception ex)
                    {
                        Logger.Error($"TaskQueue[{Name}] fallo en el temporizador [{timer.GetType().Name}]: {ex}");
                    }
                    if (timer.OneShot)
                        m_timerList.RemoveAt(i);
                }
            }


            int updatableCount = m_updatableObjects.Count;
            for (int i = 0; i < updatableCount; i++)
            {
                try { m_updatableObjects[i].Update(updateDelta); }
                catch (Exception ex)
                {
                    Logger.Error($"TaskQueue[{Name}] fallo al actualizar el objeto [{m_updatableObjects[i].GetType().Name}]: {ex}");
                }
            }


            Action msg;
            while (m_messageQueue.TryTake(out msg))
            {
                try { msg(); }
                catch (Exception ex)
                {
                    Logger.Error($"TaskQueue[{Name}] fallo al procesar un mensaje: {ex}");
                }
            }

            if (!m_running) return;

            var elapsed = m_queueTimer.ElapsedMilliseconds - timeStart;
            var waitMs = Math.Max(1, (int)(UpdateInterval - elapsed));
            try
            {
                if (m_messageQueue.TryTake(out msg, waitMs, m_cts.Token))
                {
                    try { msg(); }
                    catch (Exception ex)
                    {
                        Logger.Error($"TaskQueue[{Name}] fallo al procesar un mensaje: {ex}");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
