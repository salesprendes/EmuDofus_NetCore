using Game.Database.Structure;
using Game.Entity;
using Game.Manager;
using Game.Network;
using Game.Stats;
using System.Collections.Generic;

namespace Game.ActionEffect
{
    public sealed class PlacePrismEffect : AbstractActionEffect<PlacePrismEffect>
    {
        private const int ALIGNMENT_BONTA = 1;
        private const int ALIGNMENT_BRAKMAR = 2;
        private const int MIN_ALIGNMENT_RANK = 3;

        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, null);
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            if (character.Dishonour > 0)
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_DISHONOUR));
                return false;
            }

            if (character.AlignmentId != ALIGNMENT_BONTA && character.AlignmentId != ALIGNMENT_BRAKMAR)
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_WRONG_ALIGNMENT));
                return false;
            }

            if (character.AlignmentLevel < MIN_ALIGNMENT_RANK)
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_RANK_TOO_LOW));
                return false;
            }

            if (!character.AlignmentEnabled)
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_WINGS_NOT_ACTIVE));
                return false;
            }

            int subAreaId = character.Map?.SubArea?.Id ?? 0;

            return ConquestManager.Instance.PlacePrism(character, subAreaId);
        }
    }
}
