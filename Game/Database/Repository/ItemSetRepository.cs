using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class ItemSetRepository : Repository<ItemSetRepository, ItemSetDAO>
    {
        private Dictionary<int, ItemSetDAO> m_setById;

        public ItemSetRepository()
    : base(false, true)
        {
            m_setById = new Dictionary<int, ItemSetDAO>();
        }

        public override void OnObjectAdded(ItemSetDAO set)
        {
            m_setById.Add(set.Id, set);
        }

        public override void OnObjectRemoved(ItemSetDAO set)
        {
        }

        public ItemSetDAO GetSetById(int id)
        {
            if (m_setById.ContainsKey(id))
                return m_setById[id];
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

