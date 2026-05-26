using Protocolo.Framework.Generic;
using Protocolo.RPC.Protocol;
using Protocolo.RPC.Service;
using Game.Manager;
using System;

namespace Game.RPC
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RPCManager : Updatable
    {        
        /// <summary>
        /// 
        /// </summary>
        private readonly AuthServiceRPCConnection m_rpcConnection;
        
        /// <summary>
        /// 
        /// </summary>
        public AuthStateEnum AuthState
        {
            get;
            private set;
        }

        private int m_reconnectDelay = 5000;
        private const int ReconnectDelayMax = 30000;

        /// <summary>
        /// 
        /// </summary>
        public static RPCManager Instance => Singleton<RPCManager>.Instance;

        /// <summary>
        /// 
        /// </summary>
        public RPCManager()
        {
            AuthState = AuthStateEnum.NEGOTIATING;
            
            m_rpcConnection = new AuthServiceRPCConnection();
            m_rpcConnection.OnConnectedEvent += () =>
                {
                    AddMessage(OnConnected);
                };
            m_rpcConnection.OnDisconnectedEvent += () =>
                {
                    AddMessage(OnDisconnected);
                };
            m_rpcConnection.OnMessageEvent += (message) =>
                {
                    AddMessage(() => OnMessage(message));
                };
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            Logger.Info("RPCManager initializing...");

            Connect();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Send(AbstractRcpMessage message)
        {
            AddMessage(() =>
                {
                    m_rpcConnection.Send(message);
                });
        }

        /// <summary>
        /// 
        /// </summary>
        private void Connect()
        {
            AddMessage(() => 
            {
                Logger.Info("RPCManager connecting...");

                m_rpcConnection.Connect(WorldConfig.RPC_IP, WorldConfig.RPC_PORT);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnConnected()
        {
            Logger.Info("RPCManager connected, sending credentials.");

            AuthState = AuthStateEnum.NEGOTIATING;

            m_rpcConnection.Send(new AuthentificationMessage(WorldConfig.RPC_PASSWORD, WorldConfig.RPC_REMOTE_IP));
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnDisconnected()
        {
            Logger.Warn($"RPCManager disconnected. Reconnecting in {m_reconnectDelay / 1000}s...");
            AddTimer(m_reconnectDelay, Connect, oneshot: true);
            m_reconnectDelay = Math.Min(m_reconnectDelay * 2, ReconnectDelayMax);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void UpdateState(GameStateEnum state)
        {
            Send(new StateUpdateMessage(state));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void AccountDisconnected(long id)
        {
            Send(new AccountDisconnected(id));
        }

        public void CharacterCountChanged()
        {
            Send(new CharacterCountChangedMessage());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        private void OnMessage(AbstractRcpMessage message)
        {
            switch(message.Id)
            {
                case (int)MessageIdEnum.AUTH_TO_WORLD_CREDENTIAL_RESULT:
                    if(((AuthentificationResult)message).Result == AuthResultEnum.SUCCESS)
                    {
                        AuthState = AuthStateEnum.SUCCESS;
                        m_reconnectDelay = 5000;
                        Logger.Info("RPCManager authentification success.");
                        Send(new IdUpdateMessage(WorldConfig.GAME_ID));
                        Send(new StateUpdateMessage(GameStateEnum.ONLINE));

                        WorldService.Instance.AddMessage(() =>
                        {
                            Send(new AccountConnectedList(ClientManager.Instance.ConnectedAccounts));
                        });
                    }
                    else
                    {
                        AuthState = AuthStateEnum.FAILED;
                        Logger.Error("RPCManager authentification failed : wrong credentials.");
                    }
                    break;

                case (int)MessageIdEnum.AUTH_TO_WORLD_GAME_TICKET:
                    var ticketMessage = (GameTicketMessage)message;
                    ClientManager.Instance.AddTicket
                        (
                            ticketMessage.AccountId,
                            ticketMessage.Name,
                            ticketMessage.Pseudo,
                            ticketMessage.Power,
                            ticketMessage.RemainingSubscription,
                            ticketMessage.LastConnectionDate,
                            ticketMessage.LastConnectionIP,
                            ticketMessage.Ticket
                        );
                    break;
            }
        }       
    }
}

