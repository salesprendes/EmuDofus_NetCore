using System;
using System.Collections.Generic;

namespace Protocolo.Framework.Network
{
    public sealed class FrameManager<TClient, TMessage> : IDisposable
    {
        private TClient m_client;
        private bool m_processing;
        private List<IFrame<TClient, TMessage>> m_frames, m_framesToAdd, m_framesToRemove;

        public bool IsEmpty
        {
            get
            {
                return m_frames.Count == 0 && m_framesToAdd.Count == 0;
            }
        }

        public FrameManager(TClient client)
        {
            m_client = client;
            m_processing = false;
            m_frames = new List<IFrame<TClient,TMessage>>();
            m_framesToAdd = new List<IFrame<TClient,TMessage>>();
            m_framesToRemove = new List<IFrame<TClient,TMessage>>();
        }

        public bool HasFrame(IFrame<TClient, TMessage> frame)
        {
            return m_frames.Contains(frame) || m_framesToAdd.Contains(frame);
        }

        public bool ProcessMessage(TMessage message)
        {
            m_processing = true;
            var processed = false;

            foreach(var frame in m_frames)            
                if(frame.Process(m_client, message))
                    processed =  true;

            foreach(var frame in m_framesToAdd)
                if(!m_frames.Contains(frame))
                    m_frames.Add(frame);

            foreach(var frame in m_framesToRemove)
                if(m_frames.Contains(frame))
                    m_frames.Remove(frame);

            m_processing = false;

            m_framesToAdd.Clear();
            m_framesToRemove.Clear();
            
            return processed;
        }

        public void AddFrame(IFrame<TClient, TMessage> frame)
        {
            if (m_processing)
                m_framesToAdd.Add(frame);
            else
                if(!m_frames.Contains(frame))
                    m_frames.Add(frame);
        }

        public void RemoveFrame(IFrame<TClient, TMessage> frame)
        {
            if (m_processing)
                m_framesToRemove.Add(frame);
            else
                if(m_frames.Contains(frame))
                    m_frames.Remove(frame);
        }

        public void Dispose()
        {
            m_frames.Clear();
            m_frames = null;
            m_framesToAdd.Clear();
            m_framesToAdd = null;
            m_framesToRemove.Clear();
            m_framesToRemove = null;
        }
    }
}
