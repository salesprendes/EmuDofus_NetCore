using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Job.Skill
{
    public sealed class MagicSkill : JobSkill
    {
        public IReadOnlyList<ItemTypeEnum> TargetTypes
        {
            get;
            private set;
        }

        public MagicSkill(SkillIdEnum skill, int obtainLevel, ItemTypeEnum[] targetTypes, params int[] tools) : base(skill, obtainLevel, tools)
        {
            TargetTypes = targetTypes ?? Array.Empty<ItemTypeEnum>();
        }

        public bool CanEnhance(ItemTemplateDAO template)
        {
            return template != null && TargetTypes.Contains((ItemTypeEnum)template.Type);
        }
    }
}
