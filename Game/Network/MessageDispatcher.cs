using Protocolo.Framework.Generic;
using System;
using System.Text;

namespace Game.Network
{
    public class MessageDispatcher : Updatable
    {
        private event Action<string> OnMessage;
        private int m_cached = 0;
        private StringBuilder m_buffer = new StringBuilder();
        public bool IsConnected => OnMessage != null;

        public bool CachedBuffer
        {
            get
            {
                return m_cached > 0;
            }
            set
            {
                m_cached += value ? 1 : -1;
                if (m_cached == 0)
                {
                    if (m_buffer.Length > 0)
                    {
                        Dispatch(m_buffer.ToString());
                        m_buffer.Clear();
                    }
                }
                if (m_cached < 0)
                    throw new InvalidOperationException("cached buffer should be >= 0");
            }
        }

        public override void Dispose()
        {
            if (m_buffer != null)
                m_buffer.Clear();
            m_buffer = null;
            OnMessage = null;

            base.Dispose();
        }

        public void AddHandler(Action<string> method)
        {
            OnMessage += method;
        }

        public virtual void SafeAddHandler(Action<string> method)
        {
            AddMessage(() => { OnMessage += method; });
        }

        public virtual void RemoveHandler(Action<string> method)
        {
            OnMessage -= method;
        }

        public virtual void SafeRemoveHandler(Action<string> method)
        {
            AddMessage(() => { OnMessage -= method; });
        }

        public virtual void Dispatch(string message)
        {
            if (CachedBuffer)
            {
                m_buffer.Append(message).Append('\0');
            }
            else if (OnMessage != null)
            {
                OnMessage(message);
            }
        }


        public virtual void SafeDispatch(string message)
        {
            AddMessage(() => { if (CachedBuffer) { m_buffer.Append(message).Append('\0'); } else if (OnMessage != null) { OnMessage(message); } });
        }
    }
}

