using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Map;
using System.Collections.Generic;

namespace Game.Spawn
{
    /// <summary>
    ///
    /// </summary>
    public sealed class SpawnQueue : Updatable
    {
        private readonly List<MapInstance> m_maps;
        private readonly List<MonsterSpawnDAO> m_monsters;

        public SpawnQueue(IEnumerable<MonsterSpawnDAO> spawns)
        {
            m_maps = new List<MapInstance>();
            m_monsters = new List<MonsterSpawnDAO>(spawns);
            AddTimer(WorldConfig.SPAWN_CHECK_INTERVAL, InternalUpdate);
        }

        public void RegisterMap(MapInstance map)
        {
            AddMessage(() => m_maps.Add(map));
        }

        private void InternalUpdate()
        {
            if (m_maps.Count == 0)
                return;

            for (int pass = 0; pass < WorldConfig.SPAWN_MAX_GROUP_PER_MAP; pass++)
                for (int i = 0; i < m_maps.Count; i++)
                {
                    var map = m_maps[i];
                    // Skip maps that have been visited before but are currently empty —
                    // monsters from the initial spawn are still alive; no need to respawn.
                    if (map.IsInitialized && map.PlayerCount == 0)
                        continue;
                    map.SpawnMonsters(m_monsters);
                }
        }
    }
}
