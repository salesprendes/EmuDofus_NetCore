using Game.Database.Structure;
using Game.ActionEffect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Quest
{
    public sealed class Quest
    {
        private QuestDAO m_record;

        public int Id => m_record.Id;
        public string Description => m_record.Description;

        public List<QuestStep> Steps { get; }

        public Quest(QuestDAO record)
        {
            m_record = record;
            Steps = record.Steps.Select(stepRecord => new QuestStep(stepRecord)).OrderBy(step => step.Order).ToList();
        }
    }
}


