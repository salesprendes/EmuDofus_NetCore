using Protocolo.Framework.Generic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Protocolo.Framework.Network
{
    public abstract class AbstractTcpServer<TServer, TClient> : TaskProcessor<TServer>, IServer<TClient> where TServer : AbstractTcpServer<TServer, TClient>, new() where TClient : AbstractTcpClient<TClient>, new()
    {
        private const int ReceiveBufferSize = 8 * 1024;
        private const int InitialPooledOperations = 64;
        private const int MaximumIdleSendOperations = 1024;
        private const int MaximumIdleReceiveOperations = 1024;

        private readonly Socket m_socket;
        private readonly SocketAsyncEventArgsPool m_sendPool;
        private readonly SocketAsyncEventArgsPool m_recvPool;
        private readonly BufferManager m_bufferManager;
        private readonly ConcurrentStack<int> m_freeId;
        private readonly ConcurrentDictionary<int, TClient> m_clients;
        private int m_nextClientId;

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

        protected AbstractTcpServer() : base(typeof(TServer).Name)
        {
            m_bufferManager = new BufferManager(ReceiveBufferSize);
            m_sendPool = new SocketAsyncEventArgsPool(CreateSendSaea, ResetSendSaea, InitialPooledOperations, MaximumIdleSendOperations);
            m_recvPool = new SocketAsyncEventArgsPool(CreateRecvSaea, ResetReceiveSaea, InitialPooledOperations, MaximumIdleReceiveOperations);
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_clients = new ConcurrentDictionary<int, TClient>();
            m_freeId = new ConcurrentStack<int>();
        }

        protected void Start(string host, int port, int backLog = 500)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("El host es obligatorio.", nameof(host));

            if (port <= 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));

            Host = host;
            Port = port;
            BackLog = backLog;

            m_socket.ExclusiveAddressUse = false;
            m_socket.Bind(new IPEndPoint(SocketExtensions.ResolveIPv4Address(host), port));
            m_socket.Listen(backLog);


            var acceptWorkers = Math.Min(backLog, 20);
            for (var i = 0; i < acceptWorkers; i++)
                StartAccept(null);

            Logger.Info($"{GetType().Name} escuchando en {host}:{port}");
        }

        public void Send(TClient client, byte[] data)
        {
            if (client == null || data == null || data.Length == 0 || client.IsDisconnecting)
                return;

            if (!client.TryEnqueueSend(data))
            {
                Disconnect(client);
                return;
            }

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

        private PoolableSocketAsyncEventArgs CreateSendSaea()
        {
            var saea = new PoolableSocketAsyncEventArgs();
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
            try
            {
                if (saea.LastOperation == SocketAsyncOperation.Accept)
                    ProcessAccepted(saea);
                else if (saea.LastOperation == SocketAsyncOperation.Receive)
                    ProcessReceived(saea);
                else if (saea.LastOperation == SocketAsyncOperation.Send)
                    ProcessSent(saea);
                else if (saea.LastOperation == SocketAsyncOperation.Disconnect)
                    ProcessDisconnected(saea);
            }
            catch (Exception ex)
            {
                Logger.Error($"Excepción no controlada en el completado de IO ({saea.LastOperation}): {ex}");
                try
                {
                    if (saea.UserToken is SendState sendState)
                    {
                        var client = sendState.Client;
                        ReleaseSendSaea(saea);
                        if (client != null)
                            Disconnect(client);
                    }
                    else
                    {
                        Disconnect(saea);
                    }
                }
                catch
                {
                }
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
            
            try
            {
                while (true)
                {
                    saea.AcceptSocket = null;
                    if (m_socket.AcceptAsync(saea))
                        return;

                    HandleAccepted(saea);
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void StartReceive(PoolableSocketAsyncEventArgs saea, TClient client)
        {
            while (true)
            {
                if (client == null || client.IsDisconnecting || client.Socket == null)
                {
                    RecycleReceiveSaea(saea);
                    return;
                }

                try
                {
                    if (saea == null)
                    {
                        saea = m_recvPool.Rent();
                        saea.UserToken = client;
                    }

                    if (client.Socket.ReceiveAsync(saea))
                        return;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Error en StartReceive para {client.Ip} : {ex.Message}");
                    if (saea != null)
                        Disconnect(saea);
                    else
                        Disconnect(client);
                    return;
                }

                if (!HandleReceived(saea, client))
                    return;
            }
        }

        private bool AddClient(TClient client)
        {
            if (!m_freeId.TryPop(out var clientId))
            {
                clientId = Interlocked.Increment(ref m_nextClientId);
                if (clientId <= 0)
                    return false;
            }

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
            HandleAccepted(saea);
            StartAccept(saea);
        }

        private void HandleAccepted(SocketAsyncEventArgs saea)
        {
            var socket = saea.AcceptSocket;
            var socketError = saea.SocketError;

            if (socketError != SocketError.Success || socket == null)
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

            string ip;
            try
            {
                ip = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            }
            catch (Exception)
            {
                try { socket.Close(); } catch { }
                return;
            }

            if (!AllowConnection(ip))
            {
                try { socket.Close(); } catch { }
                return;
            }

            ConfigureClientSocket(socket);

            var client = new TClient { Socket = socket, Ip = ip, Server = this };

            if (AddClient(client))
            {
                OnClientConnected(client);
                StartReceive(null, client);
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

                OnConnectionRefused(ip);
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

            if (HandleReceived(saea, client))
                StartReceive((PoolableSocketAsyncEventArgs)saea, client);
        }

        private bool HandleReceived(SocketAsyncEventArgs saea, TClient client)
        {
            if (saea.SocketError != SocketError.Success || saea.BytesTransferred <= 0)
            {
                Disconnect(saea);
                return false;
            }

            try
            {
                OnDataReceived(client, saea.Buffer, saea.Offset, saea.BytesTransferred);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Fallo en el manejador de recepcion del socket para {client.Ip} : {ex.Message}");
                Disconnect(saea);
                return false;
            }

            return true;
        }

        private void ProcessSent(SocketAsyncEventArgs saea)
        {
            while (saea != null)
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
                        if (socket.SendAsync(saea))
                            return;
                    }
                    catch
                    {
                        ReleaseSendSaea(saea);
                        Disconnect(client);
                        return;
                    }
                }
                else
                {
                    var completedClient = sendState.Client;
                    sendState.Buffer = null;
                    sendState.Offset = 0;
                    sendState.Length = 0;

                    if (completedClient == null || completedClient.IsDisconnecting)
                    {
                        ReleaseSendSaea(saea);
                        return;
                    }

                    saea = StartQueuedSendCore(completedClient, saea);
                }
            }
        }

        private void ReleaseSendSaea(SocketAsyncEventArgs saea)
        {
            m_sendPool.Return(saea as PoolableSocketAsyncEventArgs);
        }

        private static void ResetSendSaea(PoolableSocketAsyncEventArgs saea)
        {
            saea.SetBuffer(null, 0, 0);

            var sendState = saea.UserToken as SendState;
            if (sendState == null)
                return;

            sendState.Client = null;
            sendState.Buffer = null;
            sendState.Offset = 0;
            sendState.Length = 0;
        }

        private void StartQueuedSend(TClient client, SocketAsyncEventArgs saea)
        {
            var pending = StartQueuedSendCore(client, saea);
            if (pending != null)
                ProcessSent(pending);
        }

        private SocketAsyncEventArgs StartQueuedSendCore(TClient client, SocketAsyncEventArgs saea)
        {
            var retryDequeue = true;
            while (retryDequeue)
            {
                if (client == null)
                {
                    ReleaseSendSaea(saea);
                    return null;
                }

                if (client.IsDisconnecting)
                {
                    client.ExitSendLoop();
                    ReleaseSendSaea(saea);
                    return null;
                }

                var socket = client.Socket;
                if (socket == null)
                {
                    client.ExitSendLoop();
                    ReleaseSendSaea(saea);
                    return null;
                }

                if (!client.TryDequeueSend(out var buffer))
                {
                    client.ExitSendLoop();
                    retryDequeue = client.HasPendingSend && !client.IsDisconnecting && client.TryEnterSendLoop();
                    if (!retryDequeue)
                    {
                        ReleaseSendSaea(saea);
                        return null;
                    }
                }
                else
                {
                    saea = saea ?? m_sendPool.Rent();

                    var sendState = (SendState)saea.UserToken;
                    sendState.Client = client;
                    sendState.Buffer = buffer;
                    sendState.Offset = 0;
                    sendState.Length = buffer.Length;

                    saea.SetBuffer(buffer, 0, buffer.Length);

                    try
                    {
                        return socket.SendAsync(saea) ? null : saea;
                    }
                    catch
                    {
                        ReleaseSendSaea(saea);
                        Disconnect(client);
                        return null;
                    }
                }
            }

            return null;
        }

        private void RecycleReceiveSaea(PoolableSocketAsyncEventArgs saea)
        {
            m_recvPool.Return(saea);
        }

        private static void ResetReceiveSaea(PoolableSocketAsyncEventArgs saea)
        {
            saea.UserToken = null;
        }

        private static void ConfigureClientSocket(Socket socket)
        {
            socket.ConfigureBase();
            socket.ReceiveBufferSize = 8192;
            socket.SendBufferSize = 32768;
            socket.SetAggressiveKeepAlive();
        }

        protected virtual bool AllowConnection(string ip) => true;
        protected virtual void OnConnectionRefused(string ip)
        {
        }

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
