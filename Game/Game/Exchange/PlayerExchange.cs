using Game.Database.Structure;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public sealed class PlayerExchange : AbstractEntityExchange
    {
        public PlayerExchange(CharacterEntity local, CharacterEntity distant) : base(ExchangeTypeEnum.EXCHANGE_PLAYER, local, distant) {}
    }
}


