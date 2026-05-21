using Protocolo.Framework.Database;
using System.Collections.Generic;

namespace Game.Database.Structure
{
    [Table("monsterrace")]
    public sealed class MonsterRaceDAO : DataAccessObject<MonsterRaceDAO>
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public int SuperRaceId
        {
            get;
            set;
        }

        private List<MonsterDAO> m_monsters = new List<MonsterDAO>();

        [Write(false)]
        public List<MonsterDAO> Monsters => m_monsters;
    }
}

