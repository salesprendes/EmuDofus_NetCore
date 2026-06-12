using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class StateUpdateMessage : AbstractRcpMessage
    {
        public override int Id
        {
            get
            {
                return (int)MessageIdEnum.WORLD_TO_AUTH_STATE_UPDATE;
            }
        }

        public GameStateEnum State
        {
            get;
            private set;
        }

        public StateUpdateMessage(GameStateEnum state)
        {
            State = state;
        }

        public StateUpdateMessage()
        {
        }

        public override void Deserialize()
        {
            State = (GameStateEnum)base.ReadInt();
        }

        public override void Serialize()
        {
            base.WriteInt((int)State);
        }
    }
}
