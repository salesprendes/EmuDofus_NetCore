using Game.Auction;
using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class GameAuctionHouseSellAction : AbstractGameAuctionHouseAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="npc"></param>
        public GameAuctionHouseSellAction(CharacterEntity character, NonPlayerCharacterEntity npc)
            : base(new AuctionHouseSellExchange(character, npc), character, npc)
        {
        }
    }
}


