using Protocolo.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    [Table("monstersuperrace")]
    public sealed class MonsterSuperRaceDAO : DataAccessObject<MonsterSuperRaceDAO>
    {
        private int _id;
        private string _name;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}

