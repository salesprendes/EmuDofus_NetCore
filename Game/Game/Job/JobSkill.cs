using Game.Database.Structure;
using Game.Entity;
using System.Collections.Generic;
using System.Text;

namespace Game.Job
{
    public abstract class JobSkill
    {
        public SkillIdEnum Id
        {
            get;
            private set;
        }

        public int RequiredLevel
        {
            get;
            private set;
        }

        public List<int> Tools
        {
            get;
            private set;
        }

        public JobSkill(SkillIdEnum id, int requiredLevel = 1, params int[] tools)
        {
            Id = id;
            RequiredLevel = requiredLevel;
            Tools = new List<int>(tools);
        }

        public virtual bool Usable(CharacterEntity character, int level)
        {
            return true;
        }

        public virtual void SerializeAs_SkillListMessage(int jobLevel, StringBuilder message)
        {
            message.Append((int)Id).Append('~');
            message.Append("").Append('~');
            message.Append("").Append('~');
            message.Append("").Append('~');
            message.Append("");
        }
    }
}


