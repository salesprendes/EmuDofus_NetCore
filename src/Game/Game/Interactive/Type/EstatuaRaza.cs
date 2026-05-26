using Game.Action;
using Game.Entity;
using Game.Job;
using Game.Map;
using Game.Network;

namespace Game.Interactive.Type
{
    public sealed class EstatuaRaza : InteractiveObject
    {
        public EstatuaRaza(MapInstance map, int cellId) : base(map, cellId) {}

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (skill == null || skill.Id != SkillIdEnum.SKILL_SE_RENDRE_A_INCARNAM)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!IsActive || character.MapId != Map.Id)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (character.IsGhost || character.IsTombestone)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (character.HasGameAction(GameActionTypeEnum.FIGHT) || !character.CanGameAction(GameActionTypeEnum.MAP_TELEPORT))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_YOU_ARE_AWAY));
                return;
            }
            
            if (Pathfinding.GoalDistance(Map, character.CellId, CellId) > 1)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var destMap  = WorldConfig.GetStartMap(character.Breed);
            var destCell = WorldConfig.GetStartCell(character.Breed);
            character.Teleport(destMap, destCell);
        }
    }
}
