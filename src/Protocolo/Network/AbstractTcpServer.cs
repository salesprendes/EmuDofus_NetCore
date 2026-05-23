using Protocolo.Framework.Generic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Protocolo.Framework.Network
{
    /// <summary>
    /// Base async TCP server used by Login, Game and RPC.
    /// </summary>
    public abstract class AbstractTcpServer<TServer, TClient> : TaskProcessor<TServer>, IServer<TClient>
        where TServer : AbstractTcpServer<TServer, TClient>, new()
        where TClient : AbstractTcpClient<TClient>, new()
    {
        public const int MAX_CLIENT = 10000;

        private readonly Socket m_socket;
        private readonly ObjectPool<SocketAsyncEventArgs> m_sendPool;
        private readonly ObjectPool<PoolableSocketAsyncEventArgs> m_recvPool;
        private readonly BufferManager m_bufferManager;
        private readonly ConcurrentStack<int> m_freeId;
        private readonly ConcurrentDictionary<int, TClient> m_clients;

        public string Host
        {
            get;
            private set;
        }

        public int Port
        {
            get;
            private set;
        }

        public int BackLog
        {
            get;
            private set;
        }

        public IEnumerable<TClient> Clients => m_clients.Values;

        protected AbstractTcpServer(int maxClient = MAX_CLIENT) : base(typeof(TServer).Name)
        {
            var poolSize = maxClient + 100;
            m_bufferManager = new BufferManager(1024, poolSize);
            m_sendPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendSaea, poolSize);
            m_recvPool = new ObjectPool<PoolableSocketAsyncEventArgs>(CreateRecvSaea, poolSize);
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_clients = new ConcurrentDictionary<int, TClient>();
            m_freeId = new ConcurrentStack<int>();

            for (var i = maxClient; i > 0; i--)
                m_freeId.Push(i);
        }

        protected void Start(string host, int port, int backLog = 500)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host is required.", nameof(host));

            if (port <= 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));

            Host = host;
            Port = port;
            BackLog = backLog;

            m_socket.ExclusiveAddressUse = false;
            m_socket.Bind(new IPEndPoint(ResolveHost(host), port));
            m_socket.Listen(backLog);

            // Fixed accept worker pool — backlog controls OS queue size, not worker count
            var acceptWorkers = Math.Min(backLog, 20);
            for (var i = 0; i < acceptWorkers; i++)
                StartAccept(null);

            Logger.Info(GetType().Name + " listening on " + host + ":" + port);
        }

        public void Send(TClient client, byte[] data)
        {
            if (client == null || data == null || data.Length == 0 || client.IsDisconnecting)
                return;

            client.EnqueueSend(data);
            if (client.TryEnterSendLoop())
                StartQueuedSend(client, null);
        }

        public void Disconnect(SocketAsyncEventArgs saea)
        {
            if (saea == null)
                return;

            var receiveSaea = saea as PoolableSocketAsyncEventArgs;
            var client = saea.UserToken as TClient;

            RecycleReceiveSaea(receiveSaea);

            if (client != null)
                Disconnect(client);
        }

        public void Disconnect(TClient client)
        {
            if (client == null || !client.BeginDisconnect())
                return;

            var socket = client.Socket;
            client.Socket = null;

            if (socket != null)
            {
                socket.SafeDispose();
            }

            var clientId = client.Id;
            if (clientId != -1 && m_clients.TryRemove(clientId, out var removedClient))
            {
                removedClient.Id = -1;
                m_freeId.Push(clientId);
                OnClientDisconnected(removedClient);
            }
            else
            {
                client.Id = -1;
            }
        }

        public void SendToAll(byte[] data)
        {
            foreach (var client in m_clients.Values)
                Send(client, data);
        }

        private SocketAsyncEventArgs CreateSendSaea()
        {
            var saea = new SocketAsyncEventArgs();
            saea.Completed += IOCompleted;
            saea.UserToken = new SendState();
            return saea;
        }

        private PoolableSocketAsyncEventArgs CreateRecvSaea()
        {
            var saea = new PoolableSocketAsyncEventArgs(m_bufferManager);
            saea.Completed += IOCompleted;
            return saea;
        }

        private void IOCompleted(object sender, SocketAsyncEventArgs saea)
        {
            switch (saea.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccepted(saea);
                    break;

                case SocketAsyncOperation.Receive:
                    ProcessReceived(saea);
                    break;

                case SocketAsyncOperation.Send:
                    ProcessSent(saea);
                    break;

                case SocketAsyncOperation.Disconnect:
                    ProcessDisconnected(saea);
                    break;
            }
        }

        private void ProcessDisconnected(SocketAsyncEventArgs saea)
        {
            var sendState = saea.UserToken as SendState;
            if (sendState != null)
            {
                var client = sendState.Client;
                ReleaseSendSaea(saea);
                if (client != null)
                    Disconnect(client);
                return;
            }

            Disconnect(saea);
        }

        private void StartAccept(SocketAsyncEventArgs saea)
        {
            if (saea == null)
            {
                saea = new SocketAsyncEventArgs();
                saea.Completed += IOCompleted;
            }
            else
            {
                saea.AcceptSocket = null;
            }

            try
            {
                if (!m_socket.AcceptAsync(saea))
                    ProcessAccepted(saea);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void StartReceive(PoolableSocketAsyncEventArgs saea, TClient client)
        {
            if (client == null || client.IsDisconnecting || client.Socket == null)
                return;

            if (saea == null)
            {
                saea = m_recvPool.Pop();
                saea.UserToken = client;
            }

            try
            {
                if (!client.Socket.ReceiveAsync(saea))
                    ProcessReceived(saea);
            }
            catch
            {
                Disconnect(saea);
            }
        }

        private bool AddClient(TClient client)
        {
            if (!m_freeId.TryPop(out var clientId))
                return false;

            client.Id = clientId;
            client.ResetConnectionState();
            if (m_clients.TryAdd(clientId, client))
                return true;

            client.Id = -1;
            m_freeId.Push(clientId);
            return false;
        }

        private void ProcessAccepted(SocketAsyncEventArgs saea)
        {
            var socket = saea.AcceptSocket;

            StartAccept(saea);

            if (saea.SocketError != SocketError.Success || socket == null)
            {
                try
                {
                    socket?.Close();
                }
                catch
                {
                }

                return;
            }

            var ip = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();

            if (!AllowConnection(ip))
            {
                try { socket.Close(); } catch { }
                return;
            }

            ConfigureClientSocket(socket);

            var client = new TClient
            {
                Socket = socket,
                Ip = ip,
                Server = this
            };

            if (AddClient(client))
            {
                StartReceive(null, client);
                OnClientConnected(client);
            }
            else
            {
                try
                {
                    socket.Close();
                }
                catch
                {
                }
            }
        }

        private void ProcessReceived(SocketAsyncEventArgs saea)
        {
            var client = saea.UserToken as TClient;
            if (client == null)
            {
                RecycleReceiveSaea(saea as PoolableSocketAsyncEventArgs);
                return;
            }

            if (saea.SocketError != SocketError.Success || saea.BytesTransferred <= 0)
            {
                Disconnect(saea);
                return;
            }

            try
            {
                OnDataReceived(client, saea.Buffer, saea.Offset, saea.BytesTransferred);
            }
            catch (Exception ex)
            {
                Logger.Warn("Socket receive handler failure for " + client.Ip + " : " + ex.Message);
                Disconnect(saea);
                return;
            }

            StartReceive((PoolableSocketAsyncEventArgs)saea, client);
        }

        private void ProcessSent(SocketAsyncEventArgs saea)
        {
            var sendState = (SendState)saea.UserToken;
            if (sendState == null)
            {
                ReleaseSendSaea(saea);
                return;
            }

            if (saea.SocketError != SocketError.Success || saea.BytesTransferred <= 0)
            {
                var client = sendState.Client;
                ReleaseSendSaea(saea);
                if (client != null)
                    Disconnect(client);
                return;
            }

            sendState.Offset += saea.BytesTransferred;
            sendState.Length -= saea.BytesTransferred;

            if (sendState.Length > 0)
            {
                var client = sendState.Client;
                var socket = client?.Socket;
                if (client == null || client.IsDisconnecting || socket == null)
                {
                    ReleaseSendSaea(saea);
                    return;
                }

                saea.SetBuffer(sendState.Buffer, sendState.Offset, sendState.Length);

                try
                {
                    if (!socket.SendAsync(saea))
                        ProcessSent(saea);
                }
                catch
                {
                    ReleaseSendSaea(saea);
                    Disconnect(client);
                }

                return;
            }

            var completedClient = sendState.Client;
            sendState.Buffer = null;
            sendState.Offset = 0;
            sendState.Length = 0;

            if (completedClient == null || completedClient.IsDisconnecting)
            {
                ReleaseSendSaea(saea);
                return;
            }

            StartQueuedSend(completedClient, saea);
        }

        private void ReleaseSendSaea(SocketAsyncEventArgs saea)
        {
            if (saea == null)
                return;

            saea.SetBuffer(null, 0, 0);

            var sendState = saea.UserToken as SendState;
            if (sendState != null)
            {
                sendState.Client = null;
                sendState.Buffer = null;
                sendState.Offset = 0;
                sendState.Length = 0;
            }

            m_sendPool.Push(saea);
        }

        private void StartQueuedSend(TClient client, SocketAsyncEventArgs saea)
        {
            if (client == null)
            {
                ReleaseSendSaea(saea);
                return;
            }

            if (client.IsDisconnecting)
            {
                client.ExitSendLoop();
                ReleaseSendSaea(saea);
                return;
            }

            var socket = client.Socket;
            if (socket == null)
            {
                client.ExitSendLoop();
                ReleaseSendSaea(saea);
                return;
            }

            if (!client.TryDequeueSend(out var buffer))
            {
                client.ExitSendLoop();

                if (client.HasPendingSend && !client.IsDisconnecting && client.TryEnterSendLoop())
                {
                    StartQueuedSend(client, saea);
                    return;
                }

                ReleaseSendSaea(saea);
                return;
            }

            saea = saea ?? m_sendPool.Pop();

            var sendState = (SendState)saea.UserToken;
            sendState.Client = client;
            sendState.Buffer = buffer;
            sendState.Offset = 0;
            sendState.Length = buffer.Length;

            saea.SetBuffer(buffer, 0, buffer.Length);

            try
            {
                if (!socket.SendAsync(saea))
                    ProcessSent(saea);
            }
            catch
            {
                ReleaseSendSaea(saea);
                Disconnect(client);
            }
        }

        private void RecycleReceiveSaea(PoolableSocketAsyncEventArgs saea)
        {
            if (saea == null)
                return;

            saea.UserToken = null;
            m_recvPool.Push(saea);
        }

        private static void ConfigureClientSocket(Socket socket)
        {
            socket.ConfigureBase();
            socket.ReceiveBufferSize = 8192;
            socket.SendBufferSize = 32768;
            SetAggressiveKeepAlive(socket);
        }

        private static void SetAggressiveKeepAlive(Socket socket)
        {
            if (!OperatingSystem.IsWindows())
                return;

            try
            {
                var inValue = new byte[12];
                BitConverter.GetBytes(1u).CopyTo(inValue, 0);      // enable
                BitConverter.GetBytes(60000u).CopyTo(inValue, 4);  // idle ms
                BitConverter.GetBytes(10000u).CopyTo(inValue, 8);  // interval ms
                socket.IOControl(IOControlCode.KeepAliveValues, inValue, null);
            }
            catch {}
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

        protected virtual bool AllowConnection(string ip) => true;

        protected abstract void OnClientConnected(TClient client);
        protected abstract void OnClientDisconnected(TClient client);
        protected abstract void OnDataReceived(TClient client, byte[] buffer, int offset, int count);

        private sealed class SendState
        {
            public TClient Client;
            public byte[] Buffer;
            public int Offset;
            public int Length;
        }
    }
}
