using Protocolo.Framework.Database;
using Game.Guild;
using Game.Stats;
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
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int SymbolId
        {
            get => _symbolId;
            set => SetProperty(ref _symbolId, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int SymbolColor
        {
            get => _symbolColor;
            set => SetProperty(ref _symbolColor, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int BackgroundId
        {
            get => _backgroundId;
            set => SetProperty(ref _backgroundId, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public long Experience
        {
            get => _experience;
            set => SetProperty(ref _experience, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public byte[] Stats
        {
            get => _stats;
            set => SetProperty(ref _stats, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int BoostPoint
        {
            get => _boostPoint;
            set => SetProperty(ref _boostPoint, value);
        }

        /// <summary>
        /// 
        /// </summary>
        private GuildStatistics m_statistics;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        public override void OnBeforeUpdate()
        {
            if (m_statistics != null)
                Stats = m_statistics.Serialize();
        }
    }
}


