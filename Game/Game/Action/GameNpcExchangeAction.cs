using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameNpcExchangeAction : AbstractGameExchangeAction
    {
        public GameNpcExchangeAction(CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(new NpcExchange(character, npc), character, npc)
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


