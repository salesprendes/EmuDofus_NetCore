using Protocolo.Framework.Generic;
using System;
using System.Text;

namespace Game.Network
{
    public class MessageDispatcher : Updatable
    {
        /// <summary>
        /// Umbral de auto-reparación: un tramo cacheado legítimo dura microsegundos; si el
        /// contador lleva más de esto activo es que una excepción rompió el par true/false.
        /// </summary>
        private const long StaleCachedBufferMs = 5000;

        private event Action<string> OnMessage;
        private int m_cached = 0;
        private long m_cachedSince;
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
                if (value)
                {
                    if (m_cached == 0)
                        m_cachedSince = Environment.TickCount64;
                    m_cached++;
                    return;
                }

                // Un false desbalanceado (excepción entre el par true/false) no debe tumbar
                // la conexión: se ignora en vez de lanzar.
                if (m_cached > 0)
                    m_cached--;

                if (m_cached == 0)
                    FlushCachedBuffer();
            }
        }

        private void FlushCachedBuffer()
        {
            if (m_buffer.Length > 0)
            {
                Dispatch(m_buffer.ToString());
                m_buffer.Clear();
            }
        }

        private void RecoverStaleCachedBuffer()
        {
            if (m_cached > 0 && Environment.TickCount64 - m_cachedSince > StaleCachedBufferMs)
            {
                m_cached = 0;
                FlushCachedBuffer();
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
            RecoverStaleCachedBuffer();

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
            AddMessage(() => Dispatch(message));
        }
    }
}

