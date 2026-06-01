using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Database.Structure;
using Game.Manager;
using Game.Database.Repository;

namespace Game.Quest.Impl
{
    public sealed class FindNpcObjective : AbstractQuestObjective
    {
        public int NpcTemplateId { get; }

        public FindNpcObjective(QuestObjectiveDAO record) : base(record)
        {
            try
            {
                NpcTemplateId = int.Parse(record.Parameters);
            }
            catch(Exception e)
            {
                Logger.Warn("Quest::FindNpcObjective wrong parameter type, param=" + record.Parameters, e);
            }
        }

        public override bool Done(string value)
        {
            return int.Parse(value) == NpcTemplateId;
        }
    }
}


