using Protocolo.Framework.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocolo.Framework.Network
{
    public abstract class AbstractDofusClient<T> : AbstractTcpClient<T> where T : AbstractDofusClient<T>, new()
    {
        public static bool DebugEnabled = false;
        private readonly BinaryQueue m_messageQueue;
        protected virtual int MaxMessageSize => 64 * 1024;

        public FrameManager<T, string> FrameManager
        {
            get;
            private set;
        }

        public int CumulatedPacketInOneSecond
        {
            get;
            set;
        }

        public long LastPacketTime
        {
            get;
            set;
        }

        public DateTime LastActivityTime
        {
            get;
            private set;
        }

        protected AbstractDofusClient()
        {
            m_messageQueue = new BinaryQueue();
            FrameManager = new FrameManager<T, string>((T)this);
            LastActivityTime = DateTime.UtcNow;
        }

        public IEnumerable<string> Receive(byte[] buffer, int offset, int count)
        {
            LastActivityTime = DateTime.UtcNow;
            var end = offset + count;
            var segmentStart = offset;

            foreach (var i in Enumerable.Range(offset, count).Where(i => buffer[i] == '\n' || buffer[i] == 0x00))
            {
                if (i > segmentStart)
                    m_messageQueue.WriteBytes(buffer, segmentStart, i - segmentStart);

                segmentStart = i + 1;

                if (buffer[i] == 0x00)
                {
                    if (m_messageQueue.Count > MaxMessageSize)
                    {
                        Logger.Warn("Client kicked due to oversized packet : " + Ip);
                        Disconnect();
                        yield break;
                    }

                    if (!RegisterPacketActivity())
                        yield break;

                    yield return m_messageQueue.ReadStringDirect(m_messageQueue.Count, Encoding.UTF8);
                }
            }

            if (segmentStart < end)
                m_messageQueue.WriteBytes(buffer, segmentStart, end - segmentStart);

            if (m_messageQueue.Count > MaxMessageSize)
            {
                Logger.Warn("Client kicked due to oversized packet : " + Ip);
                Disconnect();
            }
        }

        public virtual void Send(string message)
        {
            if (message == null)
                return;

            if (DebugEnabled)
                Logger.Debug("Server : " + message);

            var byteCount = Encoding.UTF8.GetByteCount(message);
            var data = new byte[byteCount + 1];
            Encoding.UTF8.GetBytes(message, 0, message.Length, data, 0);
            base.Send(data);
        }

        private bool RegisterPacketActivity()
        {
            if (Environment.TickCount - LastPacketTime < 1000)
            {
                CumulatedPacketInOneSecond++;
                if (CumulatedPacketInOneSecond > 25)
                {
                    Logger.Warn("Client kicked due to packet spam : " + Ip);
                    Disconnect();
                    return false;
                }
            }
            else
            {
                CumulatedPacketInOneSecond = 1;
                LastPacketTime = Environment.TickCount;
            }

            return true;
        }
    }
}
