using Game.Database.Structure;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public sealed class PersonalShopExchange : AbstractExchange
    {
        public CharacterEntity Character
        {
            get;
            private set;
        }

        public PersonalShopExchange(CharacterEntity character)
    : base(ExchangeTypeEnum.EXCHANGE_PERSONAL_SHOP_EDIT)
        {
            Character = character;
        }

        public override void Create()
        {
            base.Create();
            SendItemsList();
        }

        public override int AddItem(AbstractEntity actor, long guid, int quantity, long price = -1)
        {
            if (price < 1)
                return 0;

            ItemDAO item = null;
            if (quantity > 0)
            {
                item = Character.Inventory.GetItem(guid);
                if (item == null)
                {
                    base.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return 0;
                }

                if (item.RefreshTemporaryExchangeLock())
                    Character.Dispatch(WorldMessage.OBJECT_UPDATE(item));

                if (item.IsTemporarilyLockedFromExchange())
                {
                    base.Dispatch(WorldMessage.OBJECT_MOVE_ERROR());
                    return 0;
                }

                item = Character.Inventory.RemoveItem(guid, quantity);
            }
            else
                item = Character.PersonalShop.GetItem(guid);

            if (item == null)
            {
                base.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return 0;
            }

            item.MerchantPrice = price;

            if (quantity > 0)
            {
                Character.PersonalShop.AddItem(item, false);
                Character.RefreshPersonalShopTaxe();
            }

            SendItemsList();

            return item.Quantity;
        }

        public override int RemoveItem(AbstractEntity actor, long guid, int quantity)
        {
            if (quantity < 1)
                return 0;

            var item = Character.PersonalShop.RemoveItem(guid, quantity);

            if (item == null)
            {
                base.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return 0;
            }

            item.MerchantPrice = -1;

            Character.Inventory.AddItem(item);
            Character.RefreshPersonalShopTaxe();

            SendItemsList();

            return item.Quantity;
        }

        public void SendItemsList()
        {
            base.Dispatch(WorldMessage.EXCHANGE_PERSONAL_SHOP_ITEMS_LIST(Character.PersonalShop.Items));
        }
    }
}


