using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class NpcTemplateRepository : Repository<NpcTemplateRepository, NpcTemplateDAO>
    {
        private Dictionary<int, NpcTemplateDAO> m_templateById;

        public NpcTemplateRepository()
        {
            m_templateById = new Dictionary<int, NpcTemplateDAO>();
        }

        public override void OnObjectAdded(NpcTemplateDAO template)
        {
            m_templateById.Add(template.Id, template);
        }

        public override void OnObjectRemoved(NpcTemplateDAO template)
        {
            m_templateById.Remove(template.Id);
        }

        public NpcTemplateDAO GetById(int id)
        {
            if (m_templateById.ContainsKey(id))
                return m_templateById[id];
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

