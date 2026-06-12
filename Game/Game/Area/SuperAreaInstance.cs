using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Database.Structure;
using Game.Network;
using Game.Database.Repository;

namespace Game.Area
{
    public sealed class SuperAreaInstance : MessageDispatcher
    {
        private SuperAreaDAO m_superAreaRecord;
        private IEnumerable<MonsterSpawnDAO> m_spawns;

        public IEnumerable<MonsterSpawnDAO> Spawns
        {
            get
            {
                if (m_spawns == null)
                    m_spawns = MonsterSpawnRepository.Instance.GetById(ZoneTypeEnum.TYPE_SUPERAREA, m_superAreaRecord.Id);
                return m_spawns;
            }
        }

        public SuperAreaInstance(SuperAreaDAO record)
        {
            m_superAreaRecord = record;
        }
    }
}


