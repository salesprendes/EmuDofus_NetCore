using Game.Database.Repository;
using Game.Database.Structure;
using System.Collections.Generic;
using System.Text;

namespace Game.Job.Skill
{
    public sealed class CraftSkill : JobSkill
    {
        public List<ItemTemplateDAO> Craftables
        {
            get;
            private set;
        }

        public CraftSkill(SkillIdEnum skill, int obtainLevel, int[] craftables, params int[] tools) : base(skill, obtainLevel, tools)
        {
            Craftables = new List<ItemTemplateDAO>();
            foreach (var craftableItem in craftables)
            {
                var template = ItemTemplateRepository.Instance.GetById(craftableItem);
                if (template != null)
                    Craftables.Add(template);
            }
        }

        public override void SerializeAs_SkillListMessage(int jobLevel, StringBuilder message)
        {
            var maxCase = JobBook.GetCraftMaxCaseForLevel(jobLevel);
            if (maxCase > 2)
            {
                message.Append((int)Id).Append('~');
                message.Append(maxCase - 2).Append('~');
                message.Append("").Append('~');
                message.Append("").Append('~');
                message.Append("100,");
            }
            message.Append((int)Id).Append('~');
            message.Append(maxCase).Append('~');
            message.Append("").Append('~');
            message.Append("").Append('~');
            message.Append(JobBook.GetCraftSuccessPercentForLevel(jobLevel, maxCase));
        }
    }
}


