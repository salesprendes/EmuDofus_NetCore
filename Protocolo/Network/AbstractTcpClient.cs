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
        private readonly ConcurrentQueue<byte[]> m_pendingSends;

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

        internal void EnqueueSend(byte[] data)
        {
            m_pendingSends.Enqueue(data);
        }

        internal bool TryDequeueSend(out byte[] data)
        {
            return m_pendingSends.TryDequeue(out data);
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

        private void ClearPendingSends() => m_pendingSends.Clear();
    }
}
