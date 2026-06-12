using Game.Guild;
using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("guild")]
    public sealed class GuildDAO : DataAccessObject<GuildDAO>
    {
        private long _id;
        private string _name;
        private int _symbolId;
        private int _symbolColor;
        private int _backgroundId;
        private int _backgroundColor;
        private int _level;
        private long _experience;
        private byte[] _stats;
        private int _boostPoint;


        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public int SymbolId
        {
            get => _symbolId;
            set => SetProperty(ref _symbolId, value);
        }
        public int SymbolColor
        {
            get => _symbolColor;
            set => SetProperty(ref _symbolColor, value);
        }
        public int BackgroundId
        {
            get => _backgroundId;
            set => SetProperty(ref _backgroundId, value);
        }
        public int BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }
        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }
        public long Experience
        {
            get => _experience;
            set => SetProperty(ref _experience, value);
        }
        public byte[] Stats
        {
            get => _stats;
            set => SetProperty(ref _stats, value);
        }

        public int BoostPoint
        {
            get => _boostPoint;
            set => SetProperty(ref _boostPoint, value);
        }

        private GuildStatistics m_statistics;

        [Write(false)]
        public GuildStatistics Statistics
        {
            get
            {
                if (m_statistics == null)
                    m_statistics = GuildStatistics.Deserialize(Stats);
                return m_statistics;
            }
        }

        public override void OnBeforeUpdate()
        {
            if (m_statistics != null)
                Stats = m_statistics.Serialize();
        }
    }
}


