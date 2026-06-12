using Game.Auction;
using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public sealed class AuctionHouseBuyExchange : AuctionHouseExchange
    {
        public AuctionHouseBuyExchange(CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(ExchangeTypeEnum.EXCHANGE_AUCTION_HOUSE_BUY, character, npc)
        {
        }

        public override void Create()
        {
            Npc.AuctionHouse.AddHandler(Character.Dispatch);

            base.Create();
        }

        public override void Leave(bool success = false)
        {
            Npc.AuctionHouse.RemoveHandler(Character.Dispatch);

            base.Leave(success);
        }
    }
}


