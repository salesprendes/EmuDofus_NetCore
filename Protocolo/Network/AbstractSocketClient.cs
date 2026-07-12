using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Protocolo.Framework.Network
{
    public abstract class AbstractSocketClient
    {
        private const int BufferSize = 8 * 1024;

        private Socket m_socket;
        private SocketAsyncEventArgs m_connectSaea;
        private readonly PoolableSocketAsyncEventArgs m_receiveSaea;
        private readonly SocketAsyncEventArgs m_sendSaea;
        private readonly BufferManager m_bufferManager;
        private readonly ConcurrentQueue<byte[]> m_pendingSends;
        private int m_disconnectState;
        private int m_sendLoopState;
        private long m_pendingSendBytes;

        protected virtual long MaxPendingSendBytes => 4L * 1024 * 1024;

        public event Action OnConnectedEvent;
        public event Action OnDisconnectedEvent;

        public bool Connected
        {
            get
            {
                var socket = m_socket;
                return socket != null && socket.Connected && Volatile.Read(ref m_disconnectState) == 0;
            }
        }

        protected AbstractSocketClient()
        {
            m_bufferManager = new BufferManager(BufferSize, 1);
            m_receiveSaea = CreateReceiveSaea();
            m_sendSaea = CreateSendSaea();
            m_pendingSends = new ConcurrentQueue<byte[]>();

            OnConnectedEvent += OnConnected;
            OnDisconnectedEvent += OnDisconnected;
        }

        public void Connect(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host is required.", nameof(host));

            if (port <= 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));

            BeginDisconnect(false);
            CleanupConnectSaea();
            ClearPendingSends();
            OnConnecting();
            Volatile.Write(ref m_disconnectState, 0);
            Interlocked.Exchange(ref m_sendLoopState, 0);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ConfigureSocket(socket);
            m_socket = socket;

            m_connectSaea = new SocketAsyncEventArgs { RemoteEndPoint = new IPEndPoint(SocketExtensions.ResolveIPv4Address(host), port) };
            m_connectSaea.Completed += IOCompleted;

            try
            {
                if (!socket.ConnectAsync(m_connectSaea))
                    ProcessConnected(m_connectSaea);
            }
            catch
            {
                BeginDisconnect();
            }
        }

        public void Send(byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            var socket = m_socket;
            if (socket == null || Volatile.Read(ref m_disconnectState) != 0)
                return;

            if (!TryEnqueueSend(data))
            {
                BeginDisconnect();
                return;
            }

            if (TryEnterSendLoop())
                StartQueuedSend();
        }

        public void Disconnect()
        {
            BeginDisconnect();
        }

        private SocketAsyncEventArgs CreateSendSaea()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += IOCompleted;
            saea.UserToken = new SendState();
            return saea;
        }

        private PoolableSocketAsyncEventArgs CreateReceiveSaea()
        {
            var saea = new PoolableSocketAsyncEventArgs(m_bufferManager);
            saea.Completed += IOCompleted;
            return saea;
        }

        private void IOCompleted(object sender, SocketAsyncEventArgs saea)
        {
            try
            {
                if (saea.LastOperation == SocketAsyncOperation.Connect)
                    ProcessConnected(saea);
                else if (saea.LastOperation == SocketAsyncOperation.Receive)
                    ProcessReceived(saea);
                else if (saea.LastOperation == SocketAsyncOperation.Send)
                    ProcessSent(saea);
                else if (saea.LastOperation == SocketAsyncOperation.Disconnect)
                    BeginDisconnect();
            }
            catch
            {
                try
                {
                    BeginDisconnect();
                }
                catch
                {
                }
            }
        }

        private void ProcessConnected(SocketAsyncEventArgs saea)
        {
            if (saea.SocketError != SocketError.Success || m_socket == null || !m_socket.Connected)
            {
                BeginDisconnect();
                return;
            }

            StartReceive(m_receiveSaea);
            OnConnectedEvent?.Invoke();
        }

        private void ProcessReceived(SocketAsyncEventArgs saea)
        {
            if (HandleReceived(saea))
                StartReceive(saea);
        }

        private bool HandleReceived(SocketAsyncEventArgs saea)
        {
            if (saea.SocketError != SocketError.Success || saea.BytesTransferred <= 0)
            {
                BeginDisconnect();
                return false;
            }

            try
            {
                OnBytesRead(saea.Buffer, saea.Offset, saea.BytesTransferred);
            }
            catch
            {
                BeginDisconnect();
                return false;
            }

            return true;
        }

        private void ProcessSent(SocketAsyncEventArgs saea)
        {
            // Bucle en vez de recursión para los completados síncronos.
            while (true)
            {
                var sendState = saea.UserToken as SendState;
                if (sendState == null)
                    return;

                if (saea.SocketError != SocketError.Success || saea.BytesTransferred <= 0)
                {
                    ResetSendState(sendState, saea);
                    BeginDisconnect();
                    return;
                }

                sendState.Offset += saea.BytesTransferred;
                sendState.Length -= saea.BytesTransferred;

                if (sendState.Length > 0)
                {
                    saea.SetBuffer(sendState.Buffer, sendState.Offset, sendState.Length);

                    try
                    {
                        if (sendState.Socket.SendAsync(saea))
                            return;
                    }
                    catch
                    {
                        ResetSendState(sendState, saea);
                        BeginDisconnect();
                        return;
                    }
                }
                else
                {
                    ResetSendState(sendState, saea);

                    if (!StartQueuedSendCore())
                        return;
                }
            }
        }

        private void StartReceive(SocketAsyncEventArgs saea)
        {
            while (true)
            {
                var socket = m_socket;
                if (socket == null || Volatile.Read(ref m_disconnectState) != 0)
                    return;

                try
                {
                    if (socket.ReceiveAsync(saea))
                        return;
                }
                catch
                {
                    BeginDisconnect();
                    return;
                }

                if (!HandleReceived(saea))
                    return;
            }
        }

        private void StartQueuedSend()
        {
            if (StartQueuedSendCore())
                ProcessSent(m_sendSaea);
        }

        private bool StartQueuedSendCore()
        {
            var retryDequeue = true;
            while (retryDequeue)
            {
                var socket = m_socket;
                if (socket == null || Volatile.Read(ref m_disconnectState) != 0)
                {
                    ExitSendLoop();
                    return false;
                }

                if (!TryDequeueSend(out var buffer))
                {
                    ExitSendLoop();
                    retryDequeue = !m_pendingSends.IsEmpty && TryEnterSendLoop();
                    if (!retryDequeue)
                        return false;
                }
                else
                {
                    var sendState = (SendState)m_sendSaea.UserToken;
                    sendState.Socket = socket;
                    sendState.Buffer = buffer;
                    sendState.Offset = 0;
                    sendState.Length = buffer.Length;

                    m_sendSaea.SetBuffer(buffer, 0, buffer.Length);

                    try
                    {
                        return !socket.SendAsync(m_sendSaea);
                    }
                    catch
                    {
                        ResetSendState(sendState, m_sendSaea);
                        BeginDisconnect();
                        return false;
                    }
                }
            }

            return false;
        }

        private void ResetSendState(SendState sendState, SocketAsyncEventArgs saea)
        {
            if (saea != null)
                saea.SetBuffer(null, 0, 0);

            if (sendState == null)
                return;

            sendState.Buffer = null;
            sendState.Offset = 0;
            sendState.Length = 0;
            sendState.Socket = null;
        }

        private void BeginDisconnect(bool notify = true)
        {
            if (Interlocked.Exchange(ref m_disconnectState, 1) != 0)
                return;

            CleanupConnectSaea();
            ClearPendingSends();
            ExitSendLoop();
            ResetSendState(m_sendSaea.UserToken as SendState, m_sendSaea);

            var socket = Interlocked.Exchange(ref m_socket, null);
            if (socket != null)
            {
                socket.SafeDispose();
            }

            if (notify)
                OnDisconnectedEvent?.Invoke();
        }

        private void CleanupConnectSaea()
        {
            if (m_connectSaea == null)
                return;

            m_connectSaea.Completed -= IOCompleted;
            m_connectSaea.Dispose();
            m_connectSaea = null;
        }

        private bool TryEnqueueSend(byte[] data)
        {
            if (Volatile.Read(ref m_disconnectState) != 0)
                return false;

            var pendingBytes = Interlocked.Add(ref m_pendingSendBytes, data.Length);
            if (pendingBytes > MaxPendingSendBytes || Volatile.Read(ref m_disconnectState) != 0)
            {
                Interlocked.Add(ref m_pendingSendBytes, -data.Length);
                return false;
            }

            m_pendingSends.Enqueue(data);
            if (Volatile.Read(ref m_disconnectState) != 0)
            {
                ClearPendingSends();
                return false;
            }

            return true;
        }

        private bool TryDequeueSend(out byte[] data)
        {
            if (!m_pendingSends.TryDequeue(out data))
                return false;

            Interlocked.Add(ref m_pendingSendBytes, -data.Length);
            return true;
        }

        private void ClearPendingSends()
        {
            while (m_pendingSends.TryDequeue(out var data))
                Interlocked.Add(ref m_pendingSendBytes, -data.Length);
        }

        private bool TryEnterSendLoop()
        {
            return Interlocked.CompareExchange(ref m_sendLoopState, 1, 0) == 0;
        }

        private void ExitSendLoop()
        {
            Interlocked.Exchange(ref m_sendLoopState, 0);
        }

        private static void ConfigureSocket(Socket socket) => socket.ConfigureBase();
        protected virtual void OnConnecting()
        {
        }

        protected abstract void OnBytesRead(byte[] buffer, int offset, int length);
        protected abstract void OnDisconnected();
        protected abstract void OnConnected();

        private sealed class SendState
        {
            public Socket Socket;
            public byte[] Buffer;
            public int Offset;
            public int Length;
        }
    }
}
