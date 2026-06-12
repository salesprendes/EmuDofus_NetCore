using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameTaxCollectorDefenderAction : AbstractGameAction
    {
        public override bool CanAbort => true;

        public CharacterEntity Character
        {
            get;
            private set;
        }

        public GameTaxCollectorDefenderAction(CharacterEntity character)
    : base(GameActionTypeEnum.TAXCOLLECTOR_AGGRESSION, character)
        {
            Character = character;
        }

        public override void Abort(params object[] args)
        {
            base.Abort(args);
        }

        public override void Stop(params object[] args)
        {
            if (Character.GuildMember != null)
            {
                Character.SafeDispatch(WorldMessage.GUILD_TAXCOLLECTOR_DEFENDER_LEAVE(Character.GuildMember.TaxCollectorJoinedId, Character.Id));
                Character.GuildMember.TaxCollectorLeave();
            }
            base.Stop(args);
        }
    }
}


