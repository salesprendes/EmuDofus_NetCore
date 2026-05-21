using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Job.Skill
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BasicSkill : JobSkill
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skillId"></param>
        public BasicSkill(SkillIdEnum skillId)
            : base(skillId)
        {
        }
    }
}


