using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameAuctionHouseBuyAction : AbstractGameAuctionHouseAction
    {
        public GameAuctionHouseBuyAction(CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(new AuctionHouseBuyExchange(character, npc), character, npc)
        {
        }
    }
}


