using Protocolo.Framework.Generic;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Manager;
using Game.Network;
using Game.Database.Repository;

namespace Game.Area
{
    public sealed class AreaInstance : MessageDispatcher
    {
        private AreaDAO m_areaRecord;
        private SuperAreaInstance m_superArea;
        private IEnumerable<MonsterSpawnDAO> m_spawns;

        public SuperAreaInstance SuperArea
        {
            get
            {
                if (m_superArea == null)
                    m_superArea = AreaManager.Instance.GetSuperArea(m_areaRecord.SuperAreaId);
                return m_superArea;
            }
        }
        
        public IEnumerable<MonsterSpawnDAO> Spawns
        {
            get
            {
                if (m_spawns == null)
                    m_spawns = MonsterSpawnRepository.Instance.GetById(ZoneTypeEnum.TYPE_AREA, m_areaRecord.Id);
                return m_spawns;
            }
        }

        public int Id => m_areaRecord.Id;
        public string Name => m_areaRecord.Name;
        public int SuperAreaId => m_areaRecord.SuperAreaId;


        public AreaInstance(AreaDAO record)
        {
            m_areaRecord = record;
        }
    }
}


