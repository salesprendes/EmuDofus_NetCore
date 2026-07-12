using Protocolo.Framework.Generic.Logging;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;

namespace Protocolo.Framework.Network
{
    public abstract class AbstractTcpClient<T> where T : AbstractTcpClient<T>, new()
    {
        protected static ILogger Logger = LogManager.GetLogger(typeof(T));

        private int m_disconnectState;
        private int m_sendLoopState;
        private long m_pendingSendBytes;
        private readonly ConcurrentQueue<byte[]> m_pendingSends;

        protected virtual long MaxPendingSendBytes => 4L * 1024 * 1024;

        protected AbstractTcpClient()
        {
            Id = -1;
            m_pendingSends = new ConcurrentQueue<byte[]>();
        }

        public int Id
        {
            get;
            set;
        }

        public Socket Socket
        {
            get;
            set;
        }

        public IServer<T> Server
        {
            get;
            set;
        }

        public string Ip
        {
            get;
            set;
        }

        public bool IsDisconnecting => Volatile.Read(ref m_disconnectState) != 0;

        public void Send(byte[] data)
        {
            Server.Send((T)this, data);
        }

        public void Disconnect()
        {
            Server.Disconnect((T)this);
        }

        internal bool BeginDisconnect()
        {
            if (Interlocked.Exchange(ref m_disconnectState, 1) != 0)
                return false;

            ClearPendingSends();
            return true;
        }

        internal void ResetConnectionState()
        {
            Interlocked.Exchange(ref m_disconnectState, 0);
            Interlocked.Exchange(ref m_sendLoopState, 0);
            ClearPendingSends();
        }

        internal bool TryEnqueueSend(byte[] data)
        {
            if (IsDisconnecting)
                return false;

            var pendingBytes = Interlocked.Add(ref m_pendingSendBytes, data.Length);
            if (pendingBytes > MaxPendingSendBytes || IsDisconnecting)
            {
                Interlocked.Add(ref m_pendingSendBytes, -data.Length);
                return false;
            }

            m_pendingSends.Enqueue(data);
            if (IsDisconnecting)
            {
                ClearPendingSends();
                return false;
            }

            return true;
        }

        internal bool TryDequeueSend(out byte[] data)
        {
            if (!m_pendingSends.TryDequeue(out data))
                return false;

            Interlocked.Add(ref m_pendingSendBytes, -data.Length);
            return true;
        }

        internal bool HasPendingSend => !m_pendingSends.IsEmpty;

        internal bool TryEnterSendLoop()
        {
            return Interlocked.CompareExchange(ref m_sendLoopState, 1, 0) == 0;
        }

        internal void ExitSendLoop()
        {
            Interlocked.Exchange(ref m_sendLoopState, 0);
        }

        private void ClearPendingSends()
        {
            while (m_pendingSends.TryDequeue(out var data))
                Interlocked.Add(ref m_pendingSendBytes, -data.Length);
        }
    }
}
