using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Database.Structure;

namespace Game.Quest.Impl
{
    public sealed class GenericObjective : AbstractQuestObjective
    {
        public string Text => m_record.Parameters;

        public GenericObjective(QuestObjectiveDAO record) : base(record)
        {
        }

        public override bool Done(string value)
        {
            throw new NotImplementedException();
        }
    }
}


