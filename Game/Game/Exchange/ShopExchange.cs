using Game.Entity;
using Game.Network;

namespace Game.Exchange
{
    public sealed class ShopExchange : AbstractExchange
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

        public ShopExchange(CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(ExchangeTypeEnum.EXCHANGE_SHOP)
        {
            Character = character;
            Npc = npc;
        }

        public override void Create()
        {
            base.Create();
            base.Dispatch(WorldMessage.EXCHANGE_SHOP_LIST(Npc));
        }

        public override void Leave(bool success = false)
        {
            base.Leave(success);
        }

        public override void BuyItem(AbstractEntity entity, long templateId, int quantity)
        {
            if (quantity < 1)
            {
                Logger.Debug($"ShopExchange: no se puede comprar con una cantidad menor que 1: {entity.Name}");
                Character.Dispatch(WorldMessage.EXCHANGE_BUY_ERROR());
                return;
            }

            var template = Npc.ShopItems.Find(x => x.Id == templateId);
            if (template == null)
            {
                Logger.Debug($"ShopExchange: no se puede comprar porque la plantilla del objeto no existe: {entity.Name}");
                Character.Dispatch(WorldMessage.EXCHANGE_BUY_ERROR());
                return;
            }

            var price = template.Price * quantity;

            if (Character.Inventory.Kamas < price)
            {
                Logger.Debug($"ShopExchange: no hay kamas suficientes para comprar el objeto: {entity.Name}");
                Character.Dispatch(WorldMessage.EXCHANGE_BUY_ERROR());
                return;
            }

            var instance = template.Create(Character.Id, (int)Character.Type, quantity);
            if (instance == null)
            {
                Logger.Debug($"ShopExchange: error al crear el objeto de compra: {entity.Name}");
                Character.Dispatch(WorldMessage.EXCHANGE_BUY_ERROR());
                return;
            }

            Character.CachedBuffer = true;
            Character.Inventory.SubKamas(price);
            Character.Inventory.AddItem(instance);
            Character.CachedBuffer = false;
        }

        public override void SellItem(AbstractEntity entity, long guid, int quantity, long price = -1)
        {
            if (quantity < 1)
            {
                Logger.Debug($"ShopExchange: no se puede vender una cantidad menor que 1: {entity.Name}");
                entity.Dispatch(WorldMessage.EXCHANGE_SELL_ERROR());
                return;
            }

            var item = entity.Inventory.Items.Find(entry => entry.Id == guid);

            if (item == null)
            {
                Logger.Debug($"ShopExchange: no se puede vender un objeto que no existe: {entity.Name}");
                entity.Dispatch(WorldMessage.EXCHANGE_SELL_ERROR());
                return;
            }

            if (item.RefreshTemporaryExchangeLock())
                entity.Dispatch(WorldMessage.OBJECT_UPDATE(item));

            if (item.IsTemporarilyLockedFromExchange())
            {
                entity.Dispatch(WorldMessage.EXCHANGE_SELL_ERROR());
                return;
            }

            if (quantity > item.Quantity)
                quantity = item.Quantity;

            var sellPrice = (item.Template.Price / 10) * quantity;

            entity.Inventory.RemoveItem(guid, quantity);
            entity.Inventory.AddKamas(sellPrice);
        }

        protected override string SerializeAs_ExchangeCreate()
        {
            return Npc.Id.ToString();
        }
    }
}


