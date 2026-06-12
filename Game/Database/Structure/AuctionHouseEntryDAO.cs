using Protocolo.Framework.Database;
using System;

namespace Game.Database.Structure
{
    [Table("auctionhouseentry")]
    public sealed class AuctionHouseEntryDAO : DataAccessObject<AuctionHouseEntryDAO>
    {
        private long _itemId;
        private int _auctionHouseId;
        private long _ownerId;
        private long _price;
        private DateTime _expireDate;


        [Key]
        public long ItemId
        {
            get => _itemId;
            set => SetProperty(ref _itemId, value);
        }

        public int AuctionHouseId
        {
            get => _auctionHouseId;
            set => SetProperty(ref _auctionHouseId, value);
        }

        public long OwnerId
        {
            get => _ownerId;
            set => SetProperty(ref _ownerId, value);
        }

        public long Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public DateTime ExpireDate
        {
            get => _expireDate;
            set => SetProperty(ref _expireDate, value);
        }
    }
}

