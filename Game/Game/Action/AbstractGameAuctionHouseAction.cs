using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public abstract class AbstractGameAuctionHouseAction : AbstractGameExchangeAction
    {
        public AuctionHouseExchange AuctionExchange
        {
            get;
            private set;
        }

        public AbstractGameAuctionHouseAction(AuctionHouseExchange exchange, CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(exchange, character, npc)
        {
            AuctionExchange = exchange;
        }

        public override void Start()
        {
            base.Accept();
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


