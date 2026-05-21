using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Quest;
using QuestDefinition = Game.Quest.Quest;
using QuestStepDefinition = Game.Quest.QuestStep;
using System.Collections.Generic;

namespace Game.Manager
{
    public sealed class QuestManager : Singleton<QuestManager>
    {
        private Dictionary<int, QuestDefinition> m_questById;
        private Dictionary<int, QuestStepDefinition> m_stepById;

        public QuestManager()
        {
            m_questById = new Dictionary<int, QuestDefinition>();
            m_stepById = new Dictionary<int, QuestStepDefinition>();
        }

        public void Initialize()
        {
            foreach (var questDAO in QuestRepository.Instance.All)
            {
                m_questById.Add(questDAO.Id, new QuestDefinition(questDAO));
            }
        }

        public void AddStep(QuestStepDefinition step)
        {
            m_stepById.Add(step.Id, step);
        }

        public QuestDefinition GetQuest(int questId)
        {
            return m_questById[questId];
        }

        public QuestStepDefinition GetStep(int stepId)
        {
            return m_stepById[stepId];
        }
    }
}
