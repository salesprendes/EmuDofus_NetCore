using Protocolo.Framework.Database;
using Game.Database.Repository;
using PropertyChanged;
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
    [AddINotifyPropertyChangedInterface]
    public sealed class AuctionHouseEntryDAO : DataAccessObject<AuctionHouseEntryDAO>
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long ItemId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int AuctionHouseId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long OwnerId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long Price
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime ExpireDate
        {
            get;
            set;
        }        
    }
}

