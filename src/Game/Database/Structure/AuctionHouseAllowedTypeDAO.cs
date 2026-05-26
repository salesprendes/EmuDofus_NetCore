using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("auctionhouseallowedtype")]
    public sealed class AuctionHouseAllowedTypeDAO : DataAccessObject<AuctionHouseAllowedTypeDAO>
    {
        private int _auctionHouseId;
        private int _templateId;


        [Key]
        public int AuctionHouseId
        {
            get => _auctionHouseId;
            set => SetProperty(ref _auctionHouseId, value);
        }

        [Key]
        public int TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
        }
    }
}

