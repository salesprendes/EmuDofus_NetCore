using Protocolo.Framework.Configuration;
using Protocolo.RPC.Protocol;
using Protocolo.RPC.Service;
using Login.Database.Repository;

namespace Login.RPC
{
    public sealed class AuthRPCService : AbstractRpcService<AuthRPCService, AuthRPCServiceClient, AuthMessageBuilder>
    {
        [Configurable("RPCServiceIP")]
        public static string RPCServiceIP = "127.0.0.1";

        [Configurable("RPCServicePort")]
        public static int RPCServicePort = 4321;

        [Configurable("RPCPassword")]
        public static string RPCPassword = "smarken";

        public AuthRPCService()
        {
            RegisterHandler((int)MessageIdEnum.WORLD_TO_AUTH_CREDENTIAL, HandleAuthentification);
            RegisterHandler((int)MessageIdEnum.WORLD_TO_AUTH_STATE_UPDATE,  HandleGameStateUpdate);
            RegisterHandler((int)MessageIdEnum.WORLD_TO_AUTH_ID_UPDATE, HandleGameIdUpdate);
            RegisterHandler((int)MessageIdEnum.WORLD_TO_AUTH_ACCOUNT_DISCONNECTED, HandleGameAccountDisconnected);
            RegisterHandler((int)MessageIdEnum.WORLD_TO_AUTH_ACCOUNT_CONNECTED_LIST, HandleAccountConnectedLists);
            RegisterHandler((int)MessageIdEnum.WORLD_TO_AUTH_CHARACTER_COUNT_CHANGED, HandleCharacterCountChanged);
        }

        public new void Start()
        {
            Start(RPCServiceIP, RPCServicePort);
        }

        protected override void OnRPCClientConnected(AuthRPCServiceClient client)
        {
        }

        protected override void OnRPCClientDisconnected(AuthRPCServiceClient client)
        {
            if (client.AuthState != AuthStateEnum.SUCCESS)
                return;

            if (client.GameId != -1)
                AuthService.Instance.DeleteWorld(client.GameId, client);

            Logger.Warn(string.Format("AuthServiceRPC [{0}][{1}] Disconnected", client.Ip, client.GameId));
        }

        protected override void OnMessageReceived(AuthRPCServiceClient client, AbstractRcpMessage message)
        {
            if (AuthService.LogDebugEnabled)
                Logger.Debug("AuthServiceRPC " + (MessageIdEnum)message.Id);
        }

        private void HandleAuthentification(AuthRPCServiceClient client, AbstractRcpMessage message)
        {
            if (client.AuthState != AuthStateEnum.NEGOTIATING)
                return;

            var result = AuthResultEnum.FAILED;
            var authMessage = (AuthentificationMessage)message;

            if (authMessage.Password == RPCPassword)
            {
                client.AuthState = AuthStateEnum.SUCCESS;
                result = AuthResultEnum.SUCCESS;

                client.RemoteIp = authMessage.RemoteIp;
                
                Logger.Info(string.Format("AuthServiceRPC [{0}] Authed sucessfully", client.Ip));
            }
            
            client.Send(new AuthentificationResult(result));                       
        }
        
        private void HandleGameIdUpdate(AuthRPCServiceClient client, AbstractRcpMessage message)
        {
            if (client.AuthState != AuthStateEnum.SUCCESS)
                return;

            var gameIdUpdateMessage = (IdUpdateMessage)message;

            client.GameId = gameIdUpdateMessage.GameId;
            AuthService.Instance.RegisterWorld(gameIdUpdateMessage.GameId, client);

            Logger.Info(string.Format("AuthServiceRPC [{0}] GameId updated to [{1}]", client.Ip, gameIdUpdateMessage.GameId));
        }

        private void HandleGameStateUpdate(AuthRPCServiceClient client, AbstractRcpMessage message)
        {
            if (client.AuthState != AuthStateEnum.SUCCESS)
                return;

            var state = ((StateUpdateMessage)message).State;

            Logger.Info(string.Format("AuthServiceRPC [{0}][{1}] GameState updated to {2}", client.Ip, client.GameId, state));

            client.GameState = state;

            AuthService.Instance.UpdateWorldState(client.GameId, state);
        }

        private void HandleGameAccountDisconnected(AuthRPCServiceClient client, AbstractRcpMessage message)
        {
            if (client.AuthState != AuthStateEnum.SUCCESS)
                return;

            var accountId = ((AccountDisconnected)message).AccountId;
            
            Logger.Info(string.Format("AuthServiceRPC [{0}][{1}] GameAccount disconnected accountId={2}", client.Ip, client.GameId, accountId));

            AuthService.Instance.AddMessage(() => client.Players.Remove(accountId));
        }

        private void HandleCharacterCountChanged(AuthRPCServiceClient client, AbstractRcpMessage message)
        {
            if (client.AuthState != AuthStateEnum.SUCCESS)
                return;

            CharacterInstanceRepository.Instance.Reload();
        }

        private void HandleAccountConnectedLists(AuthRPCServiceClient client, AbstractRcpMessage message)
        {
            if (client.AuthState != AuthStateEnum.SUCCESS)
                return;

            var connectedList = (AccountConnectedList)message;

            Logger.Info(string.Format("AuthServiceRPC [{0}][{1}] GameAccount connected list, playerCount={2}", client.Ip, client.GameId, connectedList.ConnectedAccounts.Count));

            AuthService.Instance.AddMessage(() => client.Players.UnionWith(connectedList.ConnectedAccounts));
        }

    }
}
