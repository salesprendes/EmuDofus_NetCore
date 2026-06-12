using Game.Entity;
using Game.Exchange;
using Game.Mount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameMountStorageExchangeAction : AbstractGameExchangeAction
    {
        public GameMountStorageExchangeAction(CharacterEntity character, Paddock paddock)
    : base(new MountStorageExchange(), character)
        {
        }

        public override void Start()
        {
            Exchange.Create();
        }

        public override void Stop(params object[] args)
        {
            base.Leave(true);
            base.Stop(args);
        }

        public override void Abort(params object[] args)
        {
            base.Leave();
            base.Abort(args);
        }
    }
}


