using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("casas")]
    public sealed class HouseDAO : DataAccessObject<HouseDAO>
    {
        private int _id;
        private int _mapIdInside;
        private int _mapIdOutside;
        private int _cellIdOutside;
        private int _cellIdInside;
        private long _ownerId = -1;
        private string _lockCode = "-";
        private long _salePrice;
        private int _guildId = -1;
        private int _guildRights;
        private long _chestKamas;

        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int MapIdInside
        {
            get => _mapIdInside;
            set => SetProperty(ref _mapIdInside, value);
        }

        public int MapIdOutside
        {
            get => _mapIdOutside;
            set => SetProperty(ref _mapIdOutside, value);
        }

        public int CellIdOutside
        {
            get => _cellIdOutside;
            set => SetProperty(ref _cellIdOutside, value);
        }

        public int CellIdInside
        {
            get => _cellIdInside;
            set => SetProperty(ref _cellIdInside, value);
        }

        public long OwnerId
        {
            get => _ownerId;
            set => SetProperty(ref _ownerId, value);
        }

        public string LockCode
        {
            get => _lockCode;
            set => SetProperty(ref _lockCode, value);
        }

        public long SalePrice
        {
            get => _salePrice;
            set => SetProperty(ref _salePrice, value);
        }

        public int GuildId
        {
            get => _guildId;
            set => SetProperty(ref _guildId, value);
        }

        public int GuildRights
        {
            get => _guildRights;
            set => SetProperty(ref _guildRights, value);
        }

        public long ChestKamas
        {
            get => _chestKamas;
            set => SetProperty(ref _chestKamas, value);
        }
    }
}
