using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    /// <summary>
    ///
    /// </summary>
    public sealed class FightActionRepository : Repository<FightActionRepository, FightActionDAO>
    {
        private Dictionary<long, List<FightActionDAO>> m_actionsByZoneKey;

        public FightActionRepository()
        {
            m_actionsByZoneKey = new Dictionary<long, List<FightActionDAO>>();
        }

        private static long ZoneKey(ZoneTypeEnum type, int id) => ((long)type << 32) | (uint)id;

        public override void OnObjectAdded(FightActionDAO action)
        {
            var key = ZoneKey(action.Zone, action.ZoneId);
            if (!m_actionsByZoneKey.TryGetValue(key, out var list))
            {
                list = new List<FightActionDAO>();
                m_actionsByZoneKey[key] = list;
            }
            list.Add(action);
        }

        public IEnumerable<FightActionDAO> GetById(ZoneTypeEnum type, int id)
        {
            var key = ZoneKey(type, id);
            if (m_actionsByZoneKey.TryGetValue(key, out var list))
                return list;
            return Enumerable.Empty<FightActionDAO>();
        }

        public override void UpdateAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction) { }
        public override void DeleteAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction) { }
        public override void InsertAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction) { }
    }
}
