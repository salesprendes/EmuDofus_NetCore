using Game.Database.Structure;
using System;

namespace Game.Quest.Impl
{
    public sealed class KillMonsterObjective : AbstractQuestObjective
    {
        public int MonsterTemplateId { get; }
        public int Count { get; }

        public KillMonsterObjective(QuestObjectiveDAO record) : base(record)
        {
            try
            {
                MonsterTemplateId = int.Parse(record.Parameters.Split(',')[0]);
                Count = int.Parse(record.Parameters.Split(',')[1]);
            }
            catch (Exception e)
            {
                Logger.Warn("Quest::KillMonsterObjective wrong parameter type, param=" + record.Parameters, e);
            }
        }

        public override bool Done(string value) => int.Parse(value) >= Count;
    }
}


