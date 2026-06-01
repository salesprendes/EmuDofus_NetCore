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
    /// <summary>
    /// 
    /// </summary>
    public sealed  class SuperAreaInstance : MessageDispatcher
    {
        /// <summary>
        /// 
        /// </summary>
        private SuperAreaDAO m_superAreaRecord;
        /// <summary>
        /// 
        /// </summary>
        private IEnumerable<MonsterSpawnDAO> m_spawns;

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<MonsterSpawnDAO> Spawns
        {
            get
            {
                if (m_spawns == null)
                    m_spawns = MonsterSpawnRepository.Instance.GetById(ZoneTypeEnum.TYPE_SUPERAREA, m_superAreaRecord.Id);
                return m_spawns;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="record"></param>
        public SuperAreaInstance(SuperAreaDAO record)
        {
            m_superAreaRecord = record;
        }
    }
}


