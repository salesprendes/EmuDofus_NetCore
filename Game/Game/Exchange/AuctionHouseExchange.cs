using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public abstract class AuctionHouseExchange : AbstractExchange
    {
        public CharacterEntity Character
        {
            get;
            private set;
        }


        public NonPlayerCharacterEntity Npc
        {
            get;
            private set;
        }

        public AuctionHouseExchange(ExchangeTypeEnum type, CharacterEntity character, NonPlayerCharacterEntity npc)
   : base(type)
        {
            Character = character;
            Npc = npc;
        }

        protected override string SerializeAs_ExchangeCreate()
        {
            return "1,10,100" + ";"
                + String.Join(",", Npc.AuctionHouse.AllowedTypes) + ";"
                + Npc.AuctionHouse.Taxe + ";"
                + Npc.AuctionHouse.ItemMaxLevel + ";"
                + Npc.AuctionHouse.PlayerMaxItem + ";"
                + Npc.Id + ";"
                + Npc.AuctionHouse.Timeout + "|"
                + Npc.Id;
        }
    }
}


