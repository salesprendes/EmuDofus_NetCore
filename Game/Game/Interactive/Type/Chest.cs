using Game.Entity;
using Game.House;
using Game.Job;
using Game.Map;
using Game.Network;

namespace Game.Interactive.Type
{
    public sealed class Chest : InteractiveObject
    {
        private readonly HouseInstance m_house;

        public Chest(MapInstance map, int cellId, bool canWalkThrough = false) : base(map, cellId, canWalkThrough)
        {
            m_house = map.House;
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (skill == null || m_house == null)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            switch (skill.Id)
            {
                case SkillIdEnum.SKILL_FOUILLER:
                case SkillIdEnum.SKILL_UTILISER:
                    m_house.TryOpenChest(character, "");
                    break;

                default:
                    base.UseWithSkill(character, skill);
                    break;
            }
        }
    }
}
