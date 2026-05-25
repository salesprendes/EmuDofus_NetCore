using Game.Entity;
using Game.Job;
using Game.Map;
using Game.Network;

namespace Game.Interactive.Type
{
    public sealed class Pheonix : InteractiveObject
    {
        public Pheonix(MapInstance map, int cellId) : base(map, cellId){}

        public override int GetImplicitSkillId(CharacterEntity character)
        {
            return character.IsGhost ? (int)SkillIdEnum.SKILL_USE_PHOENIX : -1;
        }

        public override bool CanUseWithoutJobSkill(int skillId)
        {
            return skillId == (int)SkillIdEnum.SKILL_USE_PHOENIX;
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (skill != null && skill.Id != SkillIdEnum.SKILL_USE_PHOENIX)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            ReleasePlayer(character);
        }

        private void ReleasePlayer(CharacterEntity character)
        {
            if (!character.IsGhost)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Reborn();
        }
    }
}


