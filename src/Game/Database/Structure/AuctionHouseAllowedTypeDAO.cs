using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("auctionhouseallowedtype")]
    public sealed class AuctionHouseAllowedTypeDAO : DataAccessObject<AuctionHouseAllowedTypeDAO>
    {
        [Key]
        public int AuctionHouseId
        {
            get;
            set;
        }

        [Key]
        public int TemplateId
        {
            get;
            set;
        }
    }
}

