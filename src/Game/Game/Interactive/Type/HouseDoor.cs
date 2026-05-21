using Game.Entity;
using Game.Job;
using Game.Map;
using Game.Network;

namespace Game.Interactive.Type
{
    /// <summary>
    /// Regular house door interactive object. Animated/passable dungeon doors use AnimatedDoor.
    /// </summary>
    public sealed class HouseDoor : InteractiveObject
    {
        private const int FRAME_CLOSED = 1;

        public HouseDoor(MapInstance map, int cellId) : base(map, cellId)
        {
            m_frameId = FRAME_CLOSED;
            IsActive = true;
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (skill == null)
                return;

            switch (skill.Id)
            {
                case SkillIdEnum.SKILL_ENTRER:
                case SkillIdEnum.SKILL_OUVRIR:
                case SkillIdEnum.SKILL_SORTIR:
                case SkillIdEnum.SKILL_UTILISER:
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    break;

                default:
                    base.UseWithSkill(character, skill);
                    break;
            }
        }
    }
}
