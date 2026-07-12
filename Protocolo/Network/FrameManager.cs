using System;
using System.Collections.Generic;

namespace Protocolo.Framework.Network
{
    public sealed class FrameManager<TClient, TMessage> : IDisposable
    {
        private readonly object m_lock = new object();
        private readonly TClient m_client;
        private bool m_processing;
        private bool m_disposed;
        private readonly List<IFrame<TClient, TMessage>> m_frames, m_framesToAdd, m_framesToRemove;

        public bool IsEmpty
        {
            get
            {
                lock (m_lock)
                {
                    return m_frames.Count == 0 && m_framesToAdd.Count == 0;
                }
            }
        }

        public FrameManager(TClient client)
        {
            m_client = client;
            m_processing = false;
            m_frames = new List<IFrame<TClient, TMessage>>();
            m_framesToAdd = new List<IFrame<TClient, TMessage>>();
            m_framesToRemove = new List<IFrame<TClient, TMessage>>();
        }

        public bool HasFrame(IFrame<TClient, TMessage> frame)
        {
            lock (m_lock)
            {
                return m_frames.Contains(frame) || m_framesToAdd.Contains(frame);
            }
        }

        public bool ProcessMessage(TMessage message)
        {
            lock (m_lock)
            {
                if (m_disposed)
                    return false;

                m_processing = true;
                var processed = false;

                try
                {
                    for (var i = 0; i < m_frames.Count; i++)
                    {
                        var frame = m_frames[i];
                        if (frame.Process(m_client, message))
                            processed = true;
                    }
                }
                finally
                {
                    for (var i = 0; i < m_framesToAdd.Count; i++)
                    {
                        var frame = m_framesToAdd[i];
                        if (!m_frames.Contains(frame))
                            m_frames.Add(frame);
                    }

                    for (var i = 0; i < m_framesToRemove.Count; i++)
                        m_frames.Remove(m_framesToRemove[i]);

                    m_processing = false;

                    m_framesToAdd.Clear();
                    m_framesToRemove.Clear();
                }

                return processed;
            }
        }

        public void AddFrame(IFrame<TClient, TMessage> frame)
        {
            lock (m_lock)
            {
                if (m_disposed)
                    return;

                if (m_processing)
                {
                    m_framesToAdd.Add(frame);
                    return;
                }

                if (!m_frames.Contains(frame))
                    m_frames.Add(frame);
            }
        }

        public void RemoveFrame(IFrame<TClient, TMessage> frame)
        {
            lock (m_lock)
            {
                if (m_disposed)
                    return;

                if (m_processing)
                {
                    m_framesToRemove.Add(frame);
                    return;
                }

                m_frames.Remove(frame);
            }
        }

        public void Dispose()
        {
            lock (m_lock)
            {
                m_disposed = true;
                m_frames.Clear();
                m_framesToAdd.Clear();
                m_framesToRemove.Clear();
            }
        }
    }
}
