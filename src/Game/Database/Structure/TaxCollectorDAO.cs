using Protocolo.Framework.Database;
namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("taxcollector")]
    public sealed class TaxCollectorDAO : DataAccessObject<TaxCollectorDAO>
    {
        private long _id;
        private long _guildId;
        private long _ownerId;
        private int _firstName;
        private int _name;
        private int _skin;
        private int _skinSize;
        private int _mapId;
        private int _cellId;
        private long _kamas;
        private long _experience;


        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public long GuildId
        {
            get => _guildId;
            set => SetProperty(ref _guildId, value);
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
        public int FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Skin
        {
            get => _skin;
            set => SetProperty(ref _skin, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int SkinSize
        {
            get => _skinSize;
            set => SetProperty(ref _skinSize, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int MapId
        {
            get => _mapId;
            set => SetProperty(ref _mapId, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int CellId
        {
            get => _cellId;
            set => SetProperty(ref _cellId, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public long Kamas
        {
            get => _kamas;
            set => SetProperty(ref _kamas, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public long Experience
        {
            get => _experience;
            set => SetProperty(ref _experience, value);
        }
    }
}

