using System.Collections.Generic;
using Protocolo.Framework.Database;
using Game.Database.Structure;

namespace Game.Database.Repository
{
    public sealed class SuperAreaRepository : Repository<SuperAreaRepository, SuperAreaDAO>
    {
        private Dictionary<int, SuperAreaDAO> m_superAreaById;

        public SuperAreaRepository()
        {
            m_superAreaById = new Dictionary<int, SuperAreaDAO>();
        }

        public override void OnObjectAdded(SuperAreaDAO superArea)
        {
            m_superAreaById.Add(superArea.Id, superArea);
        }

        public override void OnObjectRemoved(SuperAreaDAO superArea)
        {
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

    public sealed class AreaRepository : Repository<AreaRepository, AreaDAO>
    {
        private Dictionary<int, AreaDAO> m_areaById;

        public AreaRepository()
        {
            m_areaById = new Dictionary<int, AreaDAO>();
        }

        public override void OnObjectAdded(AreaDAO area)
        {
            m_areaById.Add(area.Id, area);
        }

        public override void OnObjectRemoved(AreaDAO area)
        {
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

    public sealed class SubAreaRepository : Repository<SubAreaRepository, SubAreaDAO>
    {
        private Dictionary<int, SubAreaDAO> m_subAreaById;

        public SubAreaRepository()
        {
            m_subAreaById = new Dictionary<int, SubAreaDAO>();
        }

        public override void OnObjectAdded(SubAreaDAO subArea)
        {
            m_subAreaById.Add(subArea.Id, subArea);
        }

        public override void OnObjectRemoved(SubAreaDAO subArea)
        {
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

