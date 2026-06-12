using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class ItemTemplateRepository : Repository<ItemTemplateRepository, ItemTemplateDAO>
    {
        private Dictionary<int, ItemTemplateDAO> m_templateById;

        public ItemTemplateRepository()
    : base(false, true)
        {
            m_templateById = new Dictionary<int, ItemTemplateDAO>();
        }

        public override void OnObjectAdded(ItemTemplateDAO template)
        {
            m_templateById.Add(template.Id, template);
        }

        public override void OnObjectRemoved(ItemTemplateDAO template)
        {
            m_templateById.Remove(template.Id);
        }

        public ItemTemplateDAO GetById(int templateId)
        {
            if (m_templateById.ContainsKey(templateId))
                return m_templateById[templateId];
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

