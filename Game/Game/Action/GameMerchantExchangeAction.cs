using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameMerchantExchangeAction : AbstractGameExchangeAction
    {
        public MerchantEntity Merchant
        {
            get;
        }

        public CharacterEntity Character
        {
            get;
        }

        public GameMerchantExchangeAction(CharacterEntity character, MerchantEntity merchant)
    : base(new MerchantExchange(character, merchant), character, merchant)
        {
            Merchant = merchant;
            Character = character;
            Merchant.Buyers.Add(Character);
        }

        public override void Start()
        {
            Accept();
        }

        public override void Stop(params object[] args)
        {
            IsFinished = true;
            base.Leave(true);
            Merchant.Buyers.Remove(Character);
        }

        public override void Abort(params object[] args)
        {
            IsFinished = true;
            base.Leave(false);
            if (Merchant.Buyers != null)
                Merchant.Buyers.Remove(Character);
        }
    }
}


