using Game.Database.Structure;
using Game.Action;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class RecallEffect : AbstractActionEffect<RecallEffect>
    {
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, null);
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            if (!character.CanGameAction(GameActionTypeEnum.MAP_TELEPORT))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_YOU_ARE_AWAY));
                return false;
            }

            character.Teleport(character.SavedMapId, character.SavedCellId);

            return true;
        }
    }
}


