using Game.Database.Structure;
using Game.ActionEffect;
using Game.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Quest
{
    public sealed class QuestStep
    {
        public int Id => m_record.Id;
        public int QuestId => m_record.QuestId;
        public int Order => m_record.Order;
        public string Name => m_record.Name;
        public string Description => m_record.Description;
        public ActionList ActionsList => m_record.ActionsList;

        public List<AbstractQuestObjective> Objectives { get; }

        private readonly QuestStepDAO m_record;
        public QuestStep(QuestStepDAO record)
        {
            m_record = record;
            Objectives = record.Objectives.Select(AbstractQuestObjective.FromRecord).ToList();

            QuestManager.Instance.AddStep(this);
        }
    }
}


