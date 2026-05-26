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
    public sealed class ExperienceTemplateRepository : Repository<ExperienceTemplateRepository, ExperienceTemplateDAO>
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<int, ExperienceTemplateDAO> m_experienceByLevel;
        private List<ExperienceTemplateDAO> m_livingExperience;

        /// <summary>
        /// 
        /// </summary>
        public ExperienceTemplateRepository()
            : base(false, true)
        {
            m_experienceByLevel = new Dictionary<int, ExperienceTemplateDAO>();
            m_livingExperience = new List<ExperienceTemplateDAO>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public ExperienceTemplateDAO GetByLevel(int level)
        {
            if(m_experienceByLevel.ContainsKey(level))
                return m_experienceByLevel[level];
            return null;
        }

        public int GetLivingLevel(int experience)
        {
            var level = 1;

            foreach (var template in m_livingExperience)
            {
                if (experience < template.Living)
                    break;

                level = template.Level;
            }

            return level;
        }

        public int GetLivingMaxExperience()
        {
            if (m_livingExperience.Count == 0)
                return 0;

            return m_livingExperience[m_livingExperience.Count - 1].Living;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="experienceTemplate"></param>
        public override void OnObjectAdded(ExperienceTemplateDAO experienceTemplate)
        {
            m_experienceByLevel.Add(experienceTemplate.Level, experienceTemplate);
            if (experienceTemplate.Living >= 0)
                m_livingExperience.Add(experienceTemplate);
            m_livingExperience.Sort((left, right) => left.Level.CompareTo(right.Level));

            base.OnObjectAdded(experienceTemplate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public override void OnObjectRemoved(ExperienceTemplateDAO experienceTemplate)
        {
            m_experienceByLevel.Remove(experienceTemplate.Level);
            m_livingExperience.Remove(experienceTemplate);

            base.OnObjectRemoved(experienceTemplate);
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

