using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameGuildCreationAction : AbstractGameAction
    {
        public override bool CanAbort => true;

        public GameGuildCreationAction(CharacterEntity character)
    : base(GameActionTypeEnum.GUILD_CREATE, character)
        {
        }

        public override void Start()
        {
            Entity.Dispatch(WorldMessage.GUILD_CREATION_OPEN());
        }

        public override void Abort(params object[] args)
        {
            Entity.Dispatch(WorldMessage.GUILD_CREATION_CLOSE());
            base.Abort(args);
        }

        public override void Stop(params object[] args)
        {
            Entity.Dispatch(WorldMessage.GUILD_CREATION_CLOSE());
            base.Stop(args);
        }
    }
}


