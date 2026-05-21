using Protocolo.Framework.Configuration;
using Protocolo.Framework.Configuration.Providers;
using Protocolo.Framework.Network;
using Protocolo.RPC.Protocol;
using Login.Database;
using Login.Database.Repository;
using Login.Database.Structure;
using Login.Frames;
using Login.Network;
using Login.RPC;
using MySqlX.XDevAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Login
{
    public sealed class AuthService : AbstractTcpServer<AuthService, AuthClient>
    {
        public AuthService() : base(600) { }

        [Configurable("AuthServiceIP")]
        public static string AuthServiceIP = "127.0.0.1";

        [Configurable("AuthServicePort")]
        public static int AuthServicePort = 443;

        [Configurable("AuthMaxClient")]
        public static int AuthMaxClient = 100;

        [Configurable("AuthQueueRefreshInterval")]
        public static int AuthQueueRefreshInterval = 2000;

        [Configurable("AuthLoginTimeoutSeconds")]
        public static int AuthLoginTimeoutSeconds = 600;

        [Configurable("AuthMaxConnectionsPerIp")]
        public static int AuthMaxConnectionsPerIp = 5;

        [Configurable("AuthMaxConnectionsPerSecond")]
        public static int AuthMaxConnectionsPerSecond = 50;

        [Configurable("AuthMaxFailedAuthAttempts")]
        public static int AuthMaxFailedAuthAttempts = 10;

        [Configurable("AuthIpBanDurationSeconds")]
        public static int AuthIpBanDurationSeconds = 300;

        public ConfigurationManager ConfigurationManager
        {
            get;
            private set;
        }

        private readonly ConcurrentDictionary<string, int> m_connectionCountByIp = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, int> m_failedAuthByIp = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, long> m_bannedIps = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private long m_rateWindowTicks;
        private int m_rateWindowCount;
        private int m_activeAuthClientCount;
        private int m_waitingSubscriberCount;


        public void Start(string configPath)
        {
            ConfigurationManager = new ConfigurationManager();
            ConfigurationManager.RegisterAttributes();
            ConfigurationManager.Add(new JsonConfigurationProvider(configPath), true);
            ConfigurationManager.Load();
            AuthDbMgr.Instance.Initialize();
            AuthRPCService.Instance.Start();

            base.AddTimer(60000, UpdateAuth);
            base.AddTimer(AuthQueueRefreshInterval, RefreshQueue);

            base.Start(AuthServiceIP, AuthServicePort);
        }

        public void UpdateAuth()
        {
            AuthDbMgr.Instance.UpdateAll();
            CharacterInstanceRepository.Instance.Reload();
            CleanExpiredBans();
            CheckClientTimeouts();
        }

        private void CheckClientTimeouts()
        {
            var timeout = TimeSpan.FromSeconds(AuthLoginTimeoutSeconds);
            var now = DateTime.UtcNow;

            foreach (AuthClient client in base.Clients)
            {
                if ((now - client.LastActivityTime) > timeout)
                {
                    AddMessage(() => client.Send(AuthMessage.ACCOUNT_KICK_TIMEOUT));
                    client.Disconnect();
                }
            }
        }

        #region Network

        protected override bool AllowConnection(string ip)
        {
            if (m_bannedIps.TryGetValue(ip, out var banExpiry))
            {
                if (DateTime.UtcNow.Ticks < banExpiry)
                    return false;
                m_bannedIps.TryRemove(ip, out _);
            }

            var newCount = m_connectionCountByIp.AddOrUpdate(ip, 1, (_, c) => c + 1);
            if (newCount > AuthMaxConnectionsPerIp)
            {
                m_connectionCountByIp.AddOrUpdate(ip, 0, (_, c) => Math.Max(0, c - 1));
                return false;
            }

            var now = DateTime.UtcNow.Ticks;
            var windowStart = Interlocked.Read(ref m_rateWindowTicks);
            if (now - windowStart > TimeSpan.TicksPerSecond)
            {
                if (Interlocked.CompareExchange(ref m_rateWindowTicks, now, windowStart) == windowStart)
                    Interlocked.Exchange(ref m_rateWindowCount, 0);
            }
            if (Interlocked.Increment(ref m_rateWindowCount) > AuthMaxConnectionsPerSecond)
            {
                m_connectionCountByIp.AddOrUpdate(ip, 0, (_, c) => Math.Max(0, c - 1));
                return false;
            }

            return true;
        }

        public void RegisterFailedAuth(string ip)
        {
            if (ip == null) return;
            var attempts = m_failedAuthByIp.AddOrUpdate(ip, 1, (_, c) => c + 1);
            if (attempts >= AuthMaxFailedAuthAttempts)
            {
                m_bannedIps[ip] = DateTime.UtcNow.AddSeconds(AuthIpBanDurationSeconds).Ticks;
                m_failedAuthByIp.TryRemove(ip, out _);
                Logger.Info("IP banned for excessive failed auth attempts: " + ip);
            }
        }

        public void RegisterSuccessfulAuth(string ip)
        {
            if (ip != null)
                m_failedAuthByIp.TryRemove(ip, out _);
        }

        private void CleanExpiredBans()
        {
            var now = DateTime.UtcNow.Ticks;
            foreach (var kvp in m_bannedIps)
                if (now >= kvp.Value)
                    m_bannedIps.TryRemove(kvp.Key, out _);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="client"></param>
        protected override void OnClientConnected(AuthClient client)
        {
            if (Logger.IsDebugEnabled)
                Logger.Debug("Connected : " + client.Ip);

            AddMessage(() =>
            {
                m_activeAuthClientCount++;
                client.FrameManager.AddFrame(VersionFrame.Instance);
                client.AuthKey = Util.AuthKeyPool.Pop();
                client.Send(AuthMessage.HELLO_CONNECT(client.AuthKey));
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        protected override void OnClientDisconnected(AuthClient client)
        {
            m_connectionCountByIp.AddOrUpdate(client.Ip, 0, (_, c) => Math.Max(0, c - 1));

            AddMessage(() =>
                {
                    if (!client.IsWaitingAuthenticationQueue && m_activeAuthClientCount > 0)
                        m_activeAuthClientCount--;

                    if (Logger.IsDebugEnabled)
                        Logger.Debug("Disconnected : " + client.Ip);

                    if(client.AuthKey != null)
                    {
                        Util.AuthKeyPool.Push(client.AuthKey);
                    }

                    var wasQueued = RemoveWaitingAuthentification(client, false);

                    AuthService.Instance.ClientDisconnected(client);
                    PromoteWaitingAuthentifications();

                    if (wasQueued)
                        RefreshQueuePositions();
            });
        }

        protected override void OnDataReceived(AuthClient client, byte[] buffer, int offset, int count)
        {
            foreach (var message in client.Receive(buffer, offset, count))
            {
                if (Logger.IsDebugEnabled)
                    Logger.Debug("Client : " + message);

                client.FrameManager.ProcessMessage(message);
            }
        }

        public void SendToAll(string message)
        {
            if (message == null)
                return;

            base.SendToAll(EncodePacket(message));
        }

        private static byte[] EncodePacket(string message)
        {
            var byteCount = Encoding.UTF8.GetByteCount(message);
            var data = new byte[byteCount + 1];
            Encoding.UTF8.GetBytes(message, 0, message.Length, data, 0);
            data[byteCount] = 0x00;
            return data;
        }

        #endregion

        #region Authentication

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<long, AuthClient>  m_clientByAccount = new Dictionary<long, AuthClient>();

        /// <summary>
        /// 
        /// </summary>
        private sealed class WaitingAuthentification
        {
            public AuthClient Client
            {
                get;
                set;
            }

            public AccountDAO Account
            {
                get;
                set;
            }

            public bool IsSubscriber
            {
                get;
                set;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private List<WaitingAuthentification> m_waitingAuthentifications = new List<WaitingAuthentification>();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public bool IsConnected(long accountId)
        {
            if (m_clientByAccount.ContainsKey(accountId))
                return true;

            foreach (var world in m_worldById.Values)
                if (world.Players.Contains(accountId))
                    return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool TryQueueAuthentification(AuthClient client, AccountDAO account)
        {
            if (AuthMaxClient <= 0)
                return false;

            if (IsWaitingAuthentificationQueued(account.Id, client))
            {
                client.Send(AuthMessage.AUTH_FAILED_ALREADY_CONNECTED());
                return true;
            }

            if (client.IsWaitingAuthenticationQueue)
            {
                SendQueuePosition(client);
                return true;
            }

            if (m_waitingAuthentifications.Count == 0 && GetActiveAuthClientCount() <= AuthMaxClient)
                return false;

            SetWaitingAuthenticationQueue(client, true);

            var waitingAuthentification = new WaitingAuthentification()
            {
                Client = client,
                Account = account,
                IsSubscriber = IsSubscriber(account),
            };

            if (waitingAuthentification.IsSubscriber)
            {
                m_waitingSubscriberCount++;

                var index = m_waitingAuthentifications.FindIndex(entry => !entry.IsSubscriber);
                if (index >= 0)
                    m_waitingAuthentifications.Insert(index, waitingAuthentification);
                else
                    m_waitingAuthentifications.Add(waitingAuthentification);
            }
            else
            {
                m_waitingAuthentifications.Add(waitingAuthentification);
            }

            PromoteWaitingAuthentifications();
            RefreshQueuePositions();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="account"></param>
        public void AuthentifyClient(AuthClient client, AccountDAO account)
        {
            if (client == null || account == null)
                return;

            SetWaitingAuthenticationQueue(client, false);
            client.FrameManager.RemoveFrame(AuthentificationFrame.Instance);
            client.Account = account;

            AuthService.Instance.ClientAuthentified(client);

            client.Send(AuthMessage.ACCOUNT_PSEUDO(account.Pseudo));
            client.Send(AuthMessage.ACCOUNT_COMMUNITY);
            client.Send(AuthMessage.WORLD_HOST_LIST(GameServerRepository.Instance.All));
            client.Send(AuthMessage.ACCOUNT_RIGHT(client.Account.Power));
            client.Send(AuthMessage.ACCOUNT_SECRET_ANSWER(account.Question ?? string.Empty));

            client.FrameManager.AddFrame(WorldSelectionFrame.Instance);

            AddMessage(TryPromoteOneFromQueue);
        }

        public void SendQueuePosition(AuthClient client)
        {
            var waitingAuthentification = m_waitingAuthentifications.Find(entry => entry.Client == client);
            if (waitingAuthentification == null)
                return;

            var totalAbo = m_waitingSubscriberCount;
            var totalNonAbo = m_waitingAuthentifications.Count - totalAbo;
            var position = m_waitingAuthentifications.IndexOf(waitingAuthentification) + 1;

            SendQueuePosition(client, position, totalAbo, totalNonAbo);
        }

        public void RefreshQueue()
        {
            AddMessage(() =>
            {
                PromoteWaitingAuthentifications();
                RefreshQueuePositions();
            });
        }

        public void ClientAuthentified(AuthClient client)
        {
            m_clientByAccount[client.Account.Id] = client;
        }

        public void ClientDisconnected(AuthClient client)
        {
            if (client.Account != null)
                m_clientByAccount.Remove(client.Account.Id);            
        }

        private int GetActiveAuthClientCount()
        {
            return m_activeAuthClientCount;
        }

        private bool IsSubscriber(AccountDAO account)
        {
            return account != null && account.RemainingSubscription > DateTime.Now;
        }

        private void TryPromoteOneFromQueue()
        {
            while (m_waitingAuthentifications.Count > 0)
            {
                var next = m_waitingAuthentifications[0];
                RemoveWaitingAuthentificationAt(0);

                if (!IsClientConnected(next.Client))
                    continue;

                if (IsConnected(next.Account.Id))
                {
                    SetWaitingAuthenticationQueue(next.Client, false);
                    next.Client.Send(AuthMessage.AUTH_FAILED_ALREADY_CONNECTED());
                    continue;
                }

                SetWaitingAuthenticationQueue(next.Client, false);
                AuthentifyClient(next.Client, next.Account);
                RefreshQueuePositions();
                return;
            }
        }

        private void PromoteWaitingAuthentifications()
        {
            if (AuthMaxClient <= 0)
                return;

            while (m_waitingAuthentifications.Count > 0 && GetActiveAuthClientCount() < AuthMaxClient)
            {
                var waitingAuthentification = m_waitingAuthentifications[0];
                RemoveWaitingAuthentificationAt(0);

                if (!IsClientConnected(waitingAuthentification.Client))
                    continue;

                if (IsConnected(waitingAuthentification.Account.Id))
                {
                    SetWaitingAuthenticationQueue(waitingAuthentification.Client, false);
                    waitingAuthentification.Client.Send(AuthMessage.AUTH_FAILED_ALREADY_CONNECTED());
                    continue;
                }

                SetWaitingAuthenticationQueue(waitingAuthentification.Client, false);
                AuthentifyClient(waitingAuthentification.Client, waitingAuthentification.Account);
            }
        }

        private bool RemoveWaitingAuthentification(AuthClient client, bool restoreActiveCount = true)
        {
            for (var i = 0; i < m_waitingAuthentifications.Count; i++)
            {
                if (m_waitingAuthentifications[i].Client != client)
                    continue;

                RemoveWaitingAuthentificationAt(i);
                if (restoreActiveCount)
                    SetWaitingAuthenticationQueue(client, false);
                else
                    client.IsWaitingAuthenticationQueue = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        private void RefreshQueuePositions()
        {
            var totalAbo = m_waitingSubscriberCount;
            var totalNonAbo = m_waitingAuthentifications.Count - totalAbo;

            for (var i = 0; i < m_waitingAuthentifications.Count; i++)
            {
                var waitingAuthentification = m_waitingAuthentifications[i];

                if (!IsClientConnected(waitingAuthentification.Client))
                {
                    RemoveWaitingAuthentificationAt(i);
                    i--;
                    totalAbo = m_waitingSubscriberCount;
                    totalNonAbo = m_waitingAuthentifications.Count - totalAbo;
                    continue;
                }

                SendQueuePosition(waitingAuthentification.Client, i + 1, totalAbo, totalNonAbo);
            }
        }

        private void SendQueuePosition(AuthClient client, int position, int totalAbo, int totalNonAbo)
        {
            client.Send(AuthMessage.AUTH_QUEUE_POSITION(position, totalAbo, totalNonAbo));
        }

        private bool IsWaitingAuthentificationQueued(long accountId, AuthClient exceptClient)
        {
            for (var i = 0; i < m_waitingAuthentifications.Count; i++)
            {
                var entry = m_waitingAuthentifications[i];
                if (entry.Account.Id == accountId && entry.Client != exceptClient)
                    return true;
            }

            return false;
        }

        private void RemoveWaitingAuthentificationAt(int index)
        {
            var entry = m_waitingAuthentifications[index];
            if (entry.IsSubscriber && m_waitingSubscriberCount > 0)
                m_waitingSubscriberCount--;

            m_waitingAuthentifications.RemoveAt(index);
        }

        private void SetWaitingAuthenticationQueue(AuthClient client, bool waiting)
        {
            if (client.IsWaitingAuthenticationQueue == waiting)
                return;

            client.IsWaitingAuthenticationQueue = waiting;

            if (waiting)
            {
                if (m_activeAuthClientCount > 0)
                    m_activeAuthClientCount--;
                return;
            }

            if (IsClientConnected(client))
                m_activeAuthClientCount++;
        }

        private static bool IsClientConnected(AuthClient client)
        {
            return client != null && client.Id != -1 && !client.IsDisconnecting;
        }

        #endregion

        #region World Management

        private Dictionary<int, AuthRPCServiceClient> m_worldById = new Dictionary<int, AuthRPCServiceClient>();

        public AuthRPCServiceClient GetWorldConnectionById(int worldId)
        {
            AuthRPCServiceClient world;
            m_worldById.TryGetValue(worldId, out world);
            return world;
        }

        public GameServerDAO GetGameServerById(int worldId)
        {
            return GameServerRepository.Instance.GetById(worldId);
        }

        public void RegisterWorld(int worldId, AuthRPCServiceClient client)
        {
            AddMessage(() =>
            {
                m_worldById[worldId] = client;
                EnsureGameServer(worldId, client);
                RefreshWorldList();
            });
        }

        public void DeleteWorld(int worldId, AuthRPCServiceClient client = null)
        {
            AddMessage(() =>
            {
                AuthRPCServiceClient currentWorld;
                if (m_worldById.TryGetValue(worldId, out currentWorld) && (client == null || ReferenceEquals(currentWorld, client)))
                    m_worldById.Remove(worldId);

                SetGameServerState(worldId, GameStateEnum.OFFLINE);
                RefreshWorldList();
            });
        }

        public void UpdateWorldState(int worldId, GameStateEnum state)
        {
            AddMessage(() =>
            {
                if (worldId < 0)
                    return;

                SetGameServerState(worldId, state);
                RefreshWorldList();
            });
        }

        private void EnsureGameServer(int worldId, AuthRPCServiceClient client)
        {
            var server = GameServerRepository.Instance.GetById(worldId);
            var ip = string.IsNullOrWhiteSpace(client.RemoteIp) ? client.Ip : client.RemoteIp;

            if (server == null)
            {
                var usedPorts = new HashSet<int>(GameServerRepository.Instance.All.Select(s => s.Port));
                var port = 5555;
                while (usedPorts.Contains(port))
                    port++;

                GameServerRepository.Instance.Created(new GameServerDAO()
                {
                    Id = worldId,
                    Port = port,
                    State = 0,
                    Sub = 0,
                    FreePlaces = 500,
                    Ip = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip,
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(server.Ip) && !string.IsNullOrWhiteSpace(ip))
                server.Ip = ip;
        }

        private void SetGameServerState(int worldId, GameStateEnum state)
        {
            GameServerDAO server = GameServerRepository.Instance.GetById(worldId);

            if (server == null)
                return;

            if (server.State == (int)state)
                return;

            server.State = (int)state;
            if (server.Update())
                server.IsDirty = false;
        }

        public void SendWorldCharacterList(AuthClient client)
        {
            var accountId = client.Account.Id;
            AddMessage(() => client.Send(AuthMessage.WORLD_CHARACTER_LIST(CharacterInstanceRepository.Instance.GetCountsByServer(accountId))));
        }

        public void RefreshWorldList() => AddMessage(() => AuthService.Instance.SendToAll(AuthMessage.WORLD_HOST_LIST(GameServerRepository.Instance.All)));
        #endregion
    }
}
