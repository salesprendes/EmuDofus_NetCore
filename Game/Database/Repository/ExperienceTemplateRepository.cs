using Game.Database.Structure;
using Protocolo.Framework.Database;
using System.Collections.Generic;
using System.Linq;

namespace Game.Database.Repository
{
    public sealed class ExperienceTemplateRepository : Repository<ExperienceTemplateRepository, ExperienceTemplateDAO>
    {
        private Dictionary<int, ExperienceTemplateDAO> m_experienceByLevel;
        private List<ExperienceTemplateDAO> m_livingExperience;

        public ExperienceTemplateRepository()
            : base(false, true)
        {
            m_experienceByLevel = new Dictionary<int, ExperienceTemplateDAO>();
            m_livingExperience = new List<ExperienceTemplateDAO>();
        }

        public ExperienceTemplateDAO GetByLevel(int level)
        {
            if (m_experienceByLevel.ContainsKey(level))
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

        public long GetMaxPvpExperience()
        {
            if (m_experienceByLevel.Count == 0)
                return 0;

            return m_experienceByLevel.Values.Max(template => template.Pvp);
        }

        /// <summary>
        /// Alignment/PVP grade (1..<paramref name="maxGrade"/>) for a given honour, using the
        /// loaded PVP floors. A grade is reached when honour >= that level's PVP floor (floors are
        /// ascending), mirroring the character alignment ladder.
        /// </summary>
        public int GetPvpGrade(long honor, int maxGrade)
        {
            var grade = 1;
            for (int level = 2; level <= maxGrade; level++)
            {
                var template = GetByLevel(level);
                if (template == null || honor < template.Pvp)
                    break;

                grade = level;
            }

            return grade;
        }

        public override void OnObjectAdded(ExperienceTemplateDAO experienceTemplate)
        {
            m_experienceByLevel.Add(experienceTemplate.Level, experienceTemplate);
            if (experienceTemplate.Living >= 0)
                m_livingExperience.Add(experienceTemplate);
            m_livingExperience.Sort((left, right) => left.Level.CompareTo(right.Level));

            base.OnObjectAdded(experienceTemplate);
        }

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

