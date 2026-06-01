using System.Collections.Generic;
using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Map;

namespace Game.Manager
{
    public sealed class MapManager : Singleton<MapManager>
    {
        // All base map instances, keyed by map ID
        private readonly Dictionary<int, MapInstance> m_mapById;

        // O(1) coordinate lookup — built once in Initialize(), never mutated after
        private readonly Dictionary<long, MapInstance> m_mapByCoord;

        // Per-player creation pools: each player gets their own isolated sub-instance
        private readonly Dictionary<int, ObjectPool<MapInstance>> m_creationPools;

        // Balanced fixed instances: N pre-created copies, player routed to least populated
        private readonly Dictionary<int, List<MapInstance>> m_balancedInstances;
        private readonly HashSet<int> m_balancedIds;

        public MapManager()
        {
            m_mapById = new Dictionary<int, MapInstance>();
            m_mapByCoord = new Dictionary<long, MapInstance>();
            m_creationPools = new Dictionary<int, ObjectPool<MapInstance>>();
            m_balancedInstances = new Dictionary<int, List<MapInstance>>();
            m_balancedIds = new HashSet<int>();
        }

        private static long CoordKey(int x, int y) => ((long)x << 32) | (uint)y;

        public void Initialize()
        {
            foreach (var mapDAO in MapTemplateRepository.Instance.All)
            {
                var map = new MapInstance(mapDAO.SubAreaId, mapDAO.Id, mapDAO.X, mapDAO.Y, mapDAO.Width, mapDAO.Height, mapDAO.Data, mapDAO.DataKey, mapDAO.CreateTime, mapDAO.FightTeam0Cells, mapDAO.FightTeam1Cells);
                m_mapById.Add(map.Id, map);
                m_mapByCoord[CoordKey(map.X, map.Y)] = map;

                if (WorldConfig.MULTIPLE_INSTANCE_MAP_ID.Contains(map.Id))
                    m_creationPools.Add(map.Id, new ObjectPool<MapInstance>(map.Clone));

                if (WorldConfig.BALANCED_INSTANCE_MAPS.TryGetValue(map.Id, out int instanceCount) && instanceCount > 1)
                {
                    m_balancedIds.Add(map.Id);
                    var instances = new List<MapInstance>(instanceCount);
                    instances.Add(map);
                    for (int i = 1; i < instanceCount; i++)
                    {
                        var clone = map.Clone();
                        SpawnManager.Instance.RegisterMap(clone);
                        instances.Add(clone);
                    }
                    m_balancedInstances.Add(map.Id, instances);
                }
            }

            // Free raw Data/DataKey/CreateTime strings — only kept alive above for Clone() calls.
            // Creation-pool maps keep their Data so lazy Clone() can still work.
            foreach (var map in m_mapById.Values)
                if (!WorldConfig.MULTIPLE_INSTANCE_MAP_ID.Contains(map.Id))
                    map.FreeRawData();

            // Balanced clones are sub-instances; they won't be cloned again.
            foreach (var instances in m_balancedInstances.Values)
                for (int i = 1; i < instances.Count; i++)
                    instances[i].FreeRawData();

            Logger.Info("MapManager : " + m_mapById.Count + " MapInstance loaded.");
        }

        public MapInstance GetById(int id)
        {
            if (!m_mapById.TryGetValue(id, out var map))
                return null;

            if (WorldConfig.MULTIPLE_INSTANCE_MAP_ID.Contains(id))
                return m_creationPools[id].Pop();

            if (m_balancedIds.Contains(id))
                return GetLeastPopulated(m_balancedInstances[id]);

            return map;
        }

        public IEnumerable<MapInstance> GetByAreaId(int areaId)
        {
            foreach (var map in m_mapById.Values)
                if (map.SubArea.Area?.Id == areaId)
                    yield return map;
        }

        public MapInstance GetByCoordinates(int x, int y, int superAreaId)
        {
            if (!m_mapByCoord.TryGetValue(CoordKey(x, y), out var map))
                return null;
            return map.SubArea.Area.SuperAreaId == superAreaId ? map : null;
        }

        public void ReleaseInstance(MapInstance instance)
        {
            if (WorldConfig.MULTIPLE_INSTANCE_MAP_ID.Contains(instance.Id))
                m_creationPools[instance.Id].Push(instance);
            // Balanced and regular instances are never pushed back — they are persistent
        }

        private static MapInstance GetLeastPopulated(List<MapInstance> instances)
        {
            var best = instances[0];
            int min = best.PlayerCount;
            for (int i = 1; i < instances.Count; i++)
            {
                int count = instances[i].PlayerCount;
                if (count < min)
                {
                    min = count;
                    best = instances[i];
                }
            }
            return best;
        }
    }
}
