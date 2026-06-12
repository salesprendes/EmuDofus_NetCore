using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Job.Skill
{
    public sealed class HarvestSkill : JobSkill
    {
        public HarvestSkill(SkillIdEnum skillId, int obtainLevel, params int[] tools)
    : base(skillId, obtainLevel, tools)
        {
        }

        public override bool Usable(Entity.CharacterEntity character, int level)
        {
            var weapon = character.Inventory.Items.Find(item => item.Slot == Database.Structure.ItemSlotEnum.SLOT_WEAPON);
            var weaponId = -1;
            if (weapon != null)
                weaponId = weapon.TemplateId;
            return RequiredLevel <= level && (Tools.Count == 0 || Tools.Contains(weaponId));
        }

        public override void SerializeAs_SkillListMessage(int jobLevel, StringBuilder message)
        {
            message.Append((int)Id).Append('~');
            message.Append(JobBook.GetHarvestMinQuantityForLevel(jobLevel)).Append('~');
            message.Append(JobBook.GetHarvestMaxQuantityForLevel(jobLevel)).Append('~');
            message.Append("").Append('~');
            message.Append(JobBook.GetHarvestDurationForLevel(jobLevel));
        }
    }
}


