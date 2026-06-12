using Game.Auction;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public sealed class AuctionHouseSellExchange : AuctionHouseExchange
    {
        public AuctionHouseSellExchange(CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(ExchangeTypeEnum.EXCHANGE_AUCTION_HOUSE_SELL, character, npc)
        {
        }

        public override void Create()
        {
            base.Create();

            Npc.AuctionHouse.SendAuctionOwnerList(Character);
        }

        public override int AddItem(AbstractEntity actor, long guid, int quantity, long price = -1)
        {
            switch (Npc.AuctionHouse.TryAdd(Character, guid, quantity, price))
            {
                case AuctionAddResultEnum.INVALID_PRICE:
                    Character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_INVALID_PRICE));
                    break;

                case AuctionAddResultEnum.INVALID_TYPE:
                    Character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_AUCTION_ADD_INVALID_TYPE));
                    break;

                case AuctionAddResultEnum.ERROR:
                case AuctionAddResultEnum.INVALID_FLOOR:
                case AuctionAddResultEnum.INVALID_ITEM:
                case AuctionAddResultEnum.INVALID_QUANTITY:
                case AuctionAddResultEnum.TOO_HIGH_LEVEL:
                    Character.Dispatch(WorldMessage.OBJECT_MOVE_ERROR());
                    break;

                case AuctionAddResultEnum.TOO_MANY_ENTRIES:
                    Character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_AUCTION_HOUSE_TOO_MANY_ITEMS));
                    break;

                case AuctionAddResultEnum.NOT_ENOUGH_KAMAS_FOR_TAXE:
                    Character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_NOT_ENOUGH_KAMAS_FOR_TAXE));
                    break;
            }

            return 0;
        }

        public override int RemoveItem(AbstractEntity actor, long guid, int quantity)
        {
            Npc.AuctionHouse.TryRemove(Character, guid);

            return 0;
        }
    }
}


