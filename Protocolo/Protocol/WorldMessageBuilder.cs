using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class WorldMessageBuilder : RpcMessageBuilder
    {
        public WorldMessageBuilder()
        {
            base.Register<AuthentificationResult>((int)MessageIdEnum.AUTH_TO_WORLD_CREDENTIAL_RESULT);
            base.Register<GameTicketMessage>((int)MessageIdEnum.AUTH_TO_WORLD_GAME_TICKET);
        }
    }
}
