using Protocolo.Framework.Generic.Logging;
using System;

namespace Protocolo.Framework.Generic
{
    public sealed class UpdatableTimer
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(UpdatableTimer));

        private readonly Action m_callback;

        public long LastActivated
        {
            get;
            set;
        }

        public int Delay
        {
            get;
            private set;
        }

        public bool OneShot
        {
            get;
            private set;
        }

        public UpdatableTimer(int delay, Action callback, bool oneshot = false)
        {
            Delay = delay;
            m_callback = callback;
            OneShot = oneshot;
        }

        public void Tick(long currentTime)
        {
            try
            {
                m_callback();
            }
            catch (Exception ex)
            {
                Logger.Error("Error al procesar la llamada del temporizador: " + ex.ToString());
            }
            finally
            {
                LastActivated = currentTime;
            }
        }
    }
}
