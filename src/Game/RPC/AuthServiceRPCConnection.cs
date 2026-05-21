using Protocolo.RPC.Protocol;
using Protocolo.RPC.Service;

namespace Game.RPC
{
    public sealed class AuthServiceRPCConnection : AbstractRcpConnection<WorldMessageBuilder>
    {
        protected override void OnMessage(AbstractRcpMessage message)
        {
        }

        protected override void OnDisconnected()
        {
        }

        protected override void OnConnected()
        {
        }
    }
}

