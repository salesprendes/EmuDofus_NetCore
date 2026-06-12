using Game.Entity;
using Game.Job;
using Game.Manager;
using Game.Map;
using Game.Network;

namespace Game.Interactive.Type
{
    public sealed class Waypoint : InteractiveObject
    {

        public Waypoint(MapInstance map, int cellId)
    : base(map, cellId)
        {
            WaypointManager.Instance.AddWaypoint(Map.Id, this);
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (character.AddWaypoint(Map.Id))
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_WAYPOINT_REGISTERED));
            }

            switch (skill.Id)
            {
                case SkillIdEnum.SKILL_SAUVEGARDER:
                    Save(character);
                    break;

                case SkillIdEnum.SKILL_UTILISER_ZAAP:
                    Use(character);
                    break;
            }
        }

        public void Save(CharacterEntity character)
        {
            character.SavedMapId = Map.Id;
            character.SavedCellId = character.CellId;
            character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_WAYPOINT_SAVED));
        }

        public void Use(CharacterEntity character)
        {
            character.WaypointStart(this);
        }
    }
}


