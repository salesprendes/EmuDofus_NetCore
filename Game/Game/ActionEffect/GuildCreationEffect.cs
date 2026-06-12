using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class GuildCreationEffect : AbstractActionEffect<GuildCreationEffect>
    {
        public override bool ProcessItem(Entity.CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            throw new NotImplementedException();
        }

        public override bool Process(Entity.CharacterEntity character, Dictionary<string, string> parameters)
        {
            if (!character.CanGameAction(Action.GameActionTypeEnum.GUILD_CREATE))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_YOU_ARE_AWAY));

                return false;
            }

            character.GuildCreationOpen();

            return true;
        }
    }
}


