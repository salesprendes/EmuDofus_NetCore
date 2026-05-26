using Protocolo.Framework.Database;
using System.Collections.Generic;

namespace Game.Database.Structure
{
    [Table("monsterrace")]
    public sealed class MonsterRaceDAO : DataAccessObject<MonsterRaceDAO>
    {
        private int _id;
        private string _name;
        private int _superRaceId;


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

        public int SuperRaceId
        {
            get => _superRaceId;
            set => SetProperty(ref _superRaceId, value);
        }

        private List<MonsterDAO> m_monsters = new List<MonsterDAO>();

        [Write(false)]
        public List<MonsterDAO> Monsters => m_monsters;
    }
}

