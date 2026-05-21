using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Protocolo.Framework.Network
{
    public abstract class AbstractSocketClient
    {
        private const int BufferSize = 1024;

        private Socket m_socket;
        private SocketAsyncEventArgs m_connectSaea;
        private readonly PoolableSocketAsyncEventArgs m_receiveSaea;
        private readonly SocketAsyncEventArgs m_sendSaea;
        private readonly BufferManager m_bufferManager;
        private readonly ConcurrentQueue<byte[]> m_pendingSends;
        private int m_disconnectState;
        private int m_sendLoopState;

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
            Volatile.Write(ref m_disconnectState, 0);
            Interlocked.Exchange(ref m_sendLoopState, 0);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ConfigureSocket(socket);
            m_socket = socket;

            m_connectSaea = new SocketAsyncEventArgs
            {
                RemoteEndPoint = new IPEndPoint(ResolveHost(host), port)
            };
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

            m_pendingSends.Enqueue(data);
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
            switch (saea.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnected(saea);
                    break;

                case SocketAsyncOperation.Receive:
                    ProcessReceived(saea);
                    break;

                case SocketAsyncOperation.Send:
                    ProcessSent(saea);
                    break;

                case SocketAsyncOperation.Disconnect:
                    BeginDisconnect();
                    break;
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
            if (saea.SocketError != SocketError.Success || saea.BytesTransferred <= 0)
            {
                BeginDisconnect();
                return;
            }

            try
            {
                OnBytesRead(saea.Buffer, saea.Offset, saea.BytesTransferred);
            }
            catch
            {
                BeginDisconnect();
                return;
            }

            StartReceive(saea);
        }

        private void ProcessSent(SocketAsyncEventArgs saea)
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
                    if (!sendState.Socket.SendAsync(saea))
                        ProcessSent(saea);
                }
                catch
                {
                    ResetSendState(sendState, saea);
                    BeginDisconnect();
                }

                return;
            }

            ResetSendState(sendState, saea);
            StartQueuedSend();
        }

        private void StartReceive(SocketAsyncEventArgs saea)
        {
            var socket = m_socket;
            if (socket == null || Volatile.Read(ref m_disconnectState) != 0)
                return;

            try
            {
                if (!socket.ReceiveAsync(saea))
                    ProcessReceived(saea);
            }
            catch
            {
                BeginDisconnect();
            }
        }

        private void StartQueuedSend()
        {
            var socket = m_socket;
            if (socket == null || Volatile.Read(ref m_disconnectState) != 0)
            {
                ExitSendLoop();
                return;
            }

            if (!m_pendingSends.TryDequeue(out var buffer))
            {
                ExitSendLoop();

                if (!m_pendingSends.IsEmpty && TryEnterSendLoop())
                    StartQueuedSend();

                return;
            }

            var sendState = (SendState)m_sendSaea.UserToken;
            sendState.Socket = socket;
            sendState.Buffer = buffer;
            sendState.Offset = 0;
            sendState.Length = buffer.Length;

            m_sendSaea.SetBuffer(buffer, 0, buffer.Length);

            try
            {
                if (!socket.SendAsync(m_sendSaea))
                    ProcessSent(m_sendSaea);
            }
            catch
            {
                ResetSendState(sendState, m_sendSaea);
                BeginDisconnect();
            }
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
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }

                try
                {
                    socket.Close();
                }
                catch
                {
                }
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

        private void ClearPendingSends()
        {
            while (m_pendingSends.TryDequeue(out _))
            {
            }
        }

        private bool TryEnterSendLoop()
        {
            return Interlocked.CompareExchange(ref m_sendLoopState, 1, 0) == 0;
        }

        private void ExitSendLoop()
        {
            Interlocked.Exchange(ref m_sendLoopState, 0);
        }

        private static void ConfigureSocket(Socket socket)
        {
            socket.NoDelay = true;
            socket.Blocking = false;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.LingerState = new LingerOption(false, 0);
        }

        private static IPAddress ResolveHost(string host)
        {
            if (IPAddress.TryParse(host, out var address))
                return address;

            var resolved = Dns.GetHostAddresses(host).FirstOrDefault(entry => entry.AddressFamily == AddressFamily.InterNetwork);
            if (resolved == null)
                throw new SocketException((int)SocketError.HostNotFound);

            return resolved;
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
