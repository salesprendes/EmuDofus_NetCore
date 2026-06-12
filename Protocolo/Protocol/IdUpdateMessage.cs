using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class IdUpdateMessage : AbstractRcpMessage
    {
        public override int Id
        {
            get
            {
                return (int)MessageIdEnum.WORLD_TO_AUTH_ID_UPDATE;
            }
        }

        public int GameId
        {
            get;
            private set;
        }

        public IdUpdateMessage(int gameId)
        {
            GameId = gameId;
        }

        public IdUpdateMessage()
        {
        }

        public override void Deserialize()
        {
            GameId = base.ReadInt();
        }

        public override void Serialize()
        {
            base.WriteInt(GameId);
        }
    }
}
