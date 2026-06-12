using Game.Database.Structure;
using Game.Entity;
using Game.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class DialogLeaveEffect : AbstractActionEffect<DialogLeaveEffect>
    {
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            throw new NotImplementedException();
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            character.StopAction(Action.GameActionTypeEnum.NPC_DIALOG);

            return true;
        }
    }
}


