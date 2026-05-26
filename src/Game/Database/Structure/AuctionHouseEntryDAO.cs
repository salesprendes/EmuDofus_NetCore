using Protocolo.Framework.Database;
using Game.Database.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("auctionhouseentry")]
    public sealed class AuctionHouseEntryDAO : DataAccessObject<AuctionHouseEntryDAO>
    {
        private long _itemId;
        private int _auctionHouseId;
        private long _ownerId;
        private long _price;
        private DateTime _expireDate;


        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long ItemId
        {
            get => _itemId;
            set => SetProperty(ref _itemId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int AuctionHouseId
        {
            get => _auctionHouseId;
            set => SetProperty(ref _auctionHouseId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public long OwnerId
        {
            get => _ownerId;
            set => SetProperty(ref _ownerId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public long Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime ExpireDate
        {
            get => _expireDate;
            set => SetProperty(ref _expireDate, value);
        }        
    }
}

