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
    [Table("characterguild")]
    public sealed class CharacterGuildDAO : DataAccessObject<CharacterGuildDAO>
    {
        private long _id;
        private long _guildId;
        private int _rank;
        private int _power;
        private int _xpSharePercent;
        private long _xpGiven;


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
        public int Rank
        {
            get => _rank;
            set => SetProperty(ref _rank, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Power
        {
            get => _power;
            set => SetProperty(ref _power, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int XPSharePercent
        {
            get => _xpSharePercent;
            set => SetProperty(ref _xpSharePercent, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public long XPGiven
        {
            get => _xpGiven;
            set => SetProperty(ref _xpGiven, value);
        }
    }
}

