using Protocolo.Framework.Database;
using Game.Database.Structure;
using System.Collections.Generic;

namespace Game.Database.Repository
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MonsterSpawnRepository : Repository<MonsterSpawnRepository, MonsterSpawnDAO>
    {
        private readonly Dictionary<long, List<MonsterSpawnDAO>> m_spawnsByZoneKey;

        public MonsterSpawnRepository()
        {
            m_spawnsByZoneKey = new Dictionary<long, List<MonsterSpawnDAO>>();
        }

        private static long ZoneKey(ZoneTypeEnum type, int id) => ((long)type << 32) | (uint)id;

        public override void OnObjectAdded(MonsterSpawnDAO spawn)
        {
            var key = ZoneKey(spawn.Type, spawn.ZoneId);
            List<MonsterSpawnDAO> list;
            if (!m_spawnsByZoneKey.TryGetValue(key, out list))
            {
                list = new List<MonsterSpawnDAO>();
                m_spawnsByZoneKey[key] = list;
            }
            list.Add(spawn);
        }

        public override void OnObjectRemoved(MonsterSpawnDAO spawn)
        {
            var key = ZoneKey(spawn.Type, spawn.ZoneId);
            List<MonsterSpawnDAO> list;
            if (m_spawnsByZoneKey.TryGetValue(key, out list))
                list.Remove(spawn);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public IEnumerable<MonsterSpawnDAO> GetById(ZoneTypeEnum type, int id)
        {
            List<MonsterSpawnDAO> list;
            if (m_spawnsByZoneKey.TryGetValue(ZoneKey(type, id), out list))
                return list;
            return new MonsterSpawnDAO[0];
        }
        
        public override void UpdateAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }

        public override void DeleteAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }

        public override void InsertAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }
    }
 
    /// <summary>
    /// 
    /// </summary>
    public sealed class MonsterSuperRaceRepository : Repository<MonsterSuperRaceRepository, MonsterSuperRaceDAO>
    {

        public override void UpdateAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }

        public override void DeleteAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }

        public override void InsertAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class MonsterRaceRepository : Repository<MonsterRaceRepository, MonsterRaceDAO>
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<int, MonsterRaceDAO> m_raceById;

        /// <summary>
        /// 
        /// </summary>
        public MonsterRaceRepository()
        {
            m_raceById = new Dictionary<int, MonsterRaceDAO>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="race"></param>
        public override void OnObjectAdded(MonsterRaceDAO race)
        {
            m_raceById.Add(race.Id, race);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MonsterRaceDAO GetById(int id)
        {
            return m_raceById[id];
        }


        public override void UpdateAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }

        public override void DeleteAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }

        public override void InsertAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class MonsterRepository : Repository<MonsterRepository, MonsterDAO>
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<int, MonsterDAO> m_monsterById;

        /// <summary>
        /// 
        /// </summary>
        public MonsterRepository()
        {
            m_monsterById = new Dictionary<int, MonsterDAO>();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="monster"></param>
        public override void OnObjectAdded(MonsterDAO monster)
        {
            m_monsterById.Add(monster.Id, monster);

            MonsterRaceRepository.Instance.GetById(monster.Race).Monsters.Add(monster);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MonsterDAO GetById(int id)
        {
            if (m_monsterById.ContainsKey(id))
                return m_monsterById[id];
            return null;
        }
        
        public override void UpdateAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }

        public override void DeleteAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }

        public override void InsertAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
        }
    }
}

