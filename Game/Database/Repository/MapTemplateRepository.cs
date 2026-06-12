using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class MapTemplateRepository : Repository<MapTemplateRepository, MapTemplateDAO>
    {
        private Dictionary<int, MapTemplateDAO> m_mapById;

        public MapTemplateRepository()
        {
            m_mapById = new Dictionary<int, MapTemplateDAO>();
        }

        public override void OnObjectAdded(MapTemplateDAO map)
        {
            m_mapById.Add(map.Id, map);
        }

        public override void OnObjectRemoved(MapTemplateDAO map)
        {
            m_mapById.Remove(map.Id);
        }

        public List<MapTemplateDAO> GetMaps()
        {
            return m_dataObjects;
        }

        public MapTemplateDAO GetById(int id)
        {
            if (m_mapById.ContainsKey(id))
                return m_mapById[id];
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

