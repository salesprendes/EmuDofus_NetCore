using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("auctionhouse")]
    public sealed class AuctionHouseDAO : DataAccessObject<AuctionHouseDAO>
    {
        private int _id;
        private int _npcId;
        private int _itemMaxLevel;
        private int _playerMaxItem;
        private long _timeout;
        private int _taxe;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int NpcId
        {
            get => _npcId;
            set => SetProperty(ref _npcId, value);
        }

        public int ItemMaxLevel
        {
            get => _itemMaxLevel;
            set => SetProperty(ref _itemMaxLevel, value);
        }

        public int PlayerMaxItem
        {
            get => _playerMaxItem;
            set => SetProperty(ref _playerMaxItem, value);
        }

        public long Timeout
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }

        public int Taxe
        {
            get => _taxe;
            set => SetProperty(ref _taxe, value);
        }
    }
}

