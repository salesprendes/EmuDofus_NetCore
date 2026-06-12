using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Map;
using Game.Spawn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Manager
{
    public sealed class SpawnManager : Singleton<SpawnManager>
    {
        private readonly Dictionary<ZoneTypeEnum, Dictionary<int, SpawnQueue>> m_spawnQueueById;

        public SpawnManager()
        {
            m_spawnQueueById = new Dictionary<ZoneTypeEnum, Dictionary<int, SpawnQueue>>();
            m_spawnQueueById.Add(ZoneTypeEnum.TYPE_SUBAREA, new Dictionary<int, SpawnQueue>());
            m_spawnQueueById.Add(ZoneTypeEnum.TYPE_AREA, new Dictionary<int, SpawnQueue>());
            m_spawnQueueById.Add(ZoneTypeEnum.TYPE_SUPERAREA, new Dictionary<int, SpawnQueue>());
            m_spawnQueueById.Add(ZoneTypeEnum.TYPE_MAP, new Dictionary<int, SpawnQueue>());
        }

        public void Initialize()
        {
            foreach (var subArea in AreaManager.Instance.SubAreas)
            {
                if (subArea.Spawns.Any())
                {
                    var spawnQueue = new SpawnQueue(subArea.Spawns);
                    subArea.AddUpdatable(spawnQueue);
                    m_spawnQueueById[ZoneTypeEnum.TYPE_SUBAREA].Add(subArea.Id, spawnQueue);
                }
            }
        }

        public void RegisterMap(MapInstance map)
        {
            if (m_spawnQueueById[ZoneTypeEnum.TYPE_SUBAREA].ContainsKey(map.SubAreaId))
                m_spawnQueueById[ZoneTypeEnum.TYPE_SUBAREA][map.SubAreaId].RegisterMap(map);
        }
    }
}


