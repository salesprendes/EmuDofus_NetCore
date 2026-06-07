using Game.Entity;
using Game.House;
using Game.Job;
using Game.Map;
using Game.Network;

namespace Game.Interactive.Type
{
    public sealed class HouseDoor : InteractiveObject
    {
        private const int FRAME_CLOSED = 1;
        private readonly HouseInstance m_house;

        public HouseDoor(MapInstance map, int cellId) : base(map, cellId)
        {
            m_frameId = FRAME_CLOSED;
            IsActive = true;
            m_house = map.House ?? map.GetHouseByOutsideCellId(cellId);
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
                case SkillIdEnum.SKILL_ENTRER:
                case SkillIdEnum.SKILL_UTILISER:
                    m_house.TryEnter(character, "");
                    break;

                case SkillIdEnum.SKILL_OUVRIR:
                    m_house.OpenProperties(character);
                    break;

                case SkillIdEnum.SKILL_ACHETER:
                    m_house.ShowBuyDialog(character);
                    break;

                case SkillIdEnum.SKILL_VENDRE:
                    m_house.ShowSellDialog(character);
                    break;

                case SkillIdEnum.SKILL_VERROUILLER:
                case SkillIdEnum.SKILL_VERROUILLER_1:
                    m_house.ShowLockDialog(character);
                    break;

                case SkillIdEnum.SKILL_DEVERROUILLER:
                case SkillIdEnum.SKILL_DEVERROUILLER_1:
                    m_house.RemoveLock(character);
                    break;

                default:
                    base.UseWithSkill(character, skill);
                    break;
            }
        }
    }
}
