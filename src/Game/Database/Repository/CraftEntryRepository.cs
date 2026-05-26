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
    public sealed class CraftEntryRepository : Repository<CraftEntryRepository, CraftEntryDAO>
    {
        public CraftEntryRepository()
            : base(false, true)
        {
        }

        public override void OnObjectAdded(CraftEntryDAO craftEntry)
        {
            ItemTemplateRepository.Instance.GetById(craftEntry.TemplateId).Ingredients.Add(craftEntry);
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

