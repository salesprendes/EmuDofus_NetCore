using Protocolo.RPC.Protocol;
using Protocolo.RPC.Service;
using System.Collections.Generic;

namespace Login.RPC
{
    public sealed class AuthRPCServiceClient : AbstractRpcClient<AuthRPCServiceClient>
    {
        public GameStateEnum GameState { get; set; }
        public AuthStateEnum AuthState { get; set; }
        public string RemoteIp { get; set; }
        public int GameId { get; set; }

        public HashSet<long> Players { get; private set; }

        public AuthRPCServiceClient()
        {
            GameState = GameStateEnum.OFFLINE;
            AuthState = AuthStateEnum.NEGOTIATING;
            GameId = -1;
            Players = new HashSet<long>();
        }
    }
}
