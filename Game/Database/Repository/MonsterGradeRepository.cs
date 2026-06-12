using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class MonsterGradeRepository : Repository<MonsterGradeRepository, MonsterGradeDAO>
    {
        private Dictionary<long, MonsterGradeDAO> m_gradeById;

        public MonsterGradeRepository()
        {
            m_gradeById = new Dictionary<long, MonsterGradeDAO>();
        }

        public MonsterGradeDAO GetById(int id)
        {
            return m_gradeById[id];
        }


        public override void OnObjectAdded(MonsterGradeDAO grade)
        {
            m_gradeById.Add(grade.Id, grade);

            MonsterRepository.Instance.GetById(grade.MonsterId).AddGrade(grade);
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

