using Game.Auction;
using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameAuctionHouseSellAction : AbstractGameAuctionHouseAction
    {
        public GameAuctionHouseSellAction(CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(new AuctionHouseSellExchange(character, npc), character, npc)
        {
        }
    }
}


