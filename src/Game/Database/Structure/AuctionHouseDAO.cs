using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("auctionhouse")]
    public sealed class AuctionHouseDAO : DataAccessObject<AuctionHouseDAO>
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public int NpcId
        {
            get;
            set;
        }

        public int ItemMaxLevel
        {
            get;
            set;
        }

        public int PlayerMaxItem
        {
            get;
            set;
        }

        public long Timeout
        {
            get;
            set;
        }

        public int Taxe
        {
            get;
            set;
        }
    }
}

