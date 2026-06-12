using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class SocialRelationRepository : Repository<SocialRelationRepository, SocialRelationDAO>
    {
        private Dictionary<long, List<SocialRelationDAO>> m_relationByAccountId;

        public SocialRelationRepository()
        {
            m_relationByAccountId = new Dictionary<long, List<SocialRelationDAO>>();
        }

        public override void OnObjectAdded(SocialRelationDAO relation)
        {
            if (!m_relationByAccountId.ContainsKey(relation.AccountId))
                m_relationByAccountId.Add(relation.AccountId, new List<SocialRelationDAO>());
            m_relationByAccountId[relation.AccountId].Add(relation);
        }

        public override void OnObjectRemoved(SocialRelationDAO relation)
        {
            m_relationByAccountId[relation.AccountId].Remove(relation);
        }

        public List<SocialRelationDAO> GetByAccountId(long accountId)
        {
            if (!m_relationByAccountId.ContainsKey(accountId))
                m_relationByAccountId.Add(accountId, new List<SocialRelationDAO>());
            return m_relationByAccountId[accountId];
        }

        public SocialRelationDAO Create(long accountId, string pseudo, int type)
        {
            var relation = new SocialRelationDAO() { AccountId = accountId, Pseudo = pseudo, TypeId = type, };

            base.Created(relation);

            return relation;
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

