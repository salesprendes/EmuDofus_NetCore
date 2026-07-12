using Protocolo.Framework.IO;
using System;
using System.Collections.Generic;
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
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset > buffer.Length - count)
                throw new ArgumentOutOfRangeException(nameof(offset));

            LastActivityTime = DateTime.UtcNow;
            var end = offset + count;
            var segmentStart = offset;
            var scanPosition = offset;
            var stopReceiving = false;
            var hasDataToScan = scanPosition < end;

            while (hasDataToScan && !stopReceiving)
            {
                var relativeDelimiter = buffer.AsSpan(scanPosition, end - scanPosition).IndexOfAny((byte)'\n', (byte)0x00);

                if (relativeDelimiter < 0)
                {
                    hasDataToScan = false;
                }
                else
                {
                    var delimiterPosition = scanPosition + relativeDelimiter;
                    var b = buffer[delimiterPosition];
                    var segmentLength = delimiterPosition - segmentStart;

                    if (b == '\n')
                    {
                        if (segmentLength > 0)
                            m_messageQueue.WriteBytes(buffer, segmentStart, segmentLength);
                    }
                    else if (m_messageQueue.Count + segmentLength > MaxMessageSize)
                    {
                        Logger.Warn($"Cliente expulsado por enviar un paquete demasiado grande: {Ip}");
                        Disconnect();
                        stopReceiving = true;
                    }
                    else if (!RegisterPacketActivity())
                    {
                        stopReceiving = true;
                    }
                    else if (m_messageQueue.Count == 0)
                    {
                        yield return segmentLength == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, segmentStart, segmentLength);
                    }
                    else
                    {
                        if (segmentLength > 0)
                            m_messageQueue.WriteBytes(buffer, segmentStart, segmentLength);

                        yield return m_messageQueue.ReadStringDirect(m_messageQueue.Count, Encoding.UTF8);
                    }

                    segmentStart = delimiterPosition + 1;
                    scanPosition = segmentStart;
                    hasDataToScan = scanPosition < end;
                }
            }

            if (!stopReceiving && segmentStart < end)
            {
                m_messageQueue.WriteBytes(buffer, segmentStart, end - segmentStart);
            }

            if (!stopReceiving && m_messageQueue.Count > MaxMessageSize)
            {
                Logger.Warn($"El cliente fue expulsado debido a un paquete de tamaño excesivo: {Ip}");
                Disconnect();
            }
        }

        public virtual void Send(string message)
        {
            if (message == null)
                return;

            if (DebugEnabled)
                Logger.Debug($"Servidor: {message}");

            Send(EncodePacket(message));
        }

        public static byte[] EncodePacket(string message)
        {
            if (message == null)
                return Array.Empty<byte>();

            var byteCount = Encoding.UTF8.GetByteCount(message);
            var data = new byte[byteCount + 1];
            Encoding.UTF8.GetBytes(message, 0, message.Length, data, 0);
            data[byteCount] = 0x00;
            return data;
        }

        private bool RegisterPacketActivity()
        {
            if (Environment.TickCount64 - LastPacketTime < 1000)
            {
                CumulatedPacketInOneSecond++;
                if (CumulatedPacketInOneSecond > 25)
                {
                    Logger.Warn($"Cliente expulsado por spam de paquetes: {Ip}");
                    Disconnect();
                    return false;
                }
            }
            else
            {
                CumulatedPacketInOneSecond = 1;
                LastPacketTime = Environment.TickCount64;
            }

            return true;
        }
    }
}
