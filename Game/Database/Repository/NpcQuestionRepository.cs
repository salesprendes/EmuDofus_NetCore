using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class NpcQuestionRepository : Repository<NpcQuestionRepository, NpcQuestionDAO>
    {
        private Dictionary<int, NpcQuestionDAO> m_questionById;

        public NpcQuestionRepository()
        {
            m_questionById = new Dictionary<int, NpcQuestionDAO>();
        }

        public override void OnObjectAdded(NpcQuestionDAO question)
        {
            m_questionById.Add(question.Id, question);

            base.OnObjectAdded(question);
        }

        public override void OnObjectRemoved(NpcQuestionDAO question)
        {
            m_questionById.Remove(question.Id);

            base.OnObjectRemoved(question);
        }

        public NpcQuestionDAO GetById(int questionId)
        {
            if (m_questionById.ContainsKey(questionId))
                return m_questionById[questionId];
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

