using Protocolo.Framework.Database;
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
    [Table("monstersuperrace")]
    public sealed class MonsterSuperRaceDAO : DataAccessObject<MonsterSuperRaceDAO>
    {
        private int _id;
        private string _name;


        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int Id
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
    }
}

