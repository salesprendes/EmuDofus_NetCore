using System;
using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class CharacterCountChangedMessage : AbstractRcpMessage
    {
        public override int Id => (int)MessageIdEnum.WORLD_TO_AUTH_CHARACTER_COUNT_CHANGED;

        public override void Serialize() { base.WriteByte(1); }

        public override void Deserialize()
        {
            var type = base.ReadByte();
            if (type != 1)
                throw new InvalidOperationException("CharacterCountChangedMessage: tipo de mensaje inválido: " + type);
        }
    }
}
