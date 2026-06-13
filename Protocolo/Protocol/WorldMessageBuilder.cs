using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class WorldMessageBuilder : RpcMessageBuilder
    {
        public WorldMessageBuilder()
        {
            Register<AuthentificationResult>((int)MessageIdEnum.AUTH_TO_WORLD_CREDENTIAL_RESULT);
            Register<GameTicketMessage>((int)MessageIdEnum.AUTH_TO_WORLD_GAME_TICKET);
        }
    }
}
