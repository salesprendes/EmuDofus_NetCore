using Game.Entity;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public sealed class MerchantExchange : AbstractExchange
    {
        public CharacterEntity Character
        {
            get;
            private set;
        }

        public MerchantEntity Merchant
        {
            get;
            private set;
        }

        public MerchantExchange(CharacterEntity character, MerchantEntity merchant)
    : base(ExchangeTypeEnum.EXCHANGE_MERCHANT)
        {
            Character = character;
            Merchant = merchant;
        }

        public override void Create()
        {
            base.Create();
            SendItemsList();
        }

        public void SendItemsList()
        {
            Character.Dispatch(WorldMessage.EXCHANGE_PERSONAL_SHOP_ITEMS_LIST(Merchant.PersonalShop.Items));
        }

        protected override string SerializeAs_ExchangeCreate()
        {
            return Merchant.Id.ToString();
        }

        public override void BuyItem(AbstractEntity actor, long itemId, int quantity)
        {
            if (!Merchant.HasGameAction(Action.GameActionTypeEnum.MAP))
            {
                Character.Dispatch(WorldMessage.EXCHANGE_BUY_ERROR());
                return;
            }

            var item = Merchant.PersonalShop.GetItem(itemId);
            if (item == null || quantity > item.Quantity)
            {
                Character.CachedBuffer = true;
                Character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_ITEM_ALREADY_SOLD));
                SendItemsList();
                Character.CachedBuffer = false;
                return;
            }

            var price = item.MerchantPrice * quantity;
            if (Character.Inventory.Kamas < price)
            {
                Character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_NOT_ENOUGH_KAMAS, price));
                return;
            }

            var removedItem = Merchant.PersonalShop.RemoveItem(itemId, (int)quantity);
            removedItem.MerchantPrice = -1;
            Merchant.Inventory.AddKamas(price);

            Character.CachedBuffer = true;
            Character.Inventory.SubKamas(price);
            Character.Inventory.AddItem(removedItem);
            SendItemsList();
            Character.CachedBuffer = false;

            if (Merchant.PersonalShop.Items.Count == 0)
            {
                EntityManager.Instance.RemoveMerchant(Merchant);
            }
        }
    }
}


