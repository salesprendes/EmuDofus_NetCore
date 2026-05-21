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
    [Table("characterguild")]
    [AddINotifyPropertyChangedInterface]
    public sealed class CharacterGuildDAO : DataAccessObject<CharacterGuildDAO>
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long Id
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long GuildId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Rank
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Power
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int XPSharePercent
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long XPGiven
        {
            get;
            set;
        }
    }
}

