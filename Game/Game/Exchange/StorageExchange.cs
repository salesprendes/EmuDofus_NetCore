using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entity.Inventory;

namespace Game.Exchange
{
    public class StorageExchange : AbstractExchange
    {
        public CharacterEntity Character
        {
            get;
            private set;
        }

        public StorageInventory Storage
        {
            get;
            private set;
        }

        public StorageExchange(CharacterEntity character, StorageInventory storage, ExchangeTypeEnum type = ExchangeTypeEnum.EXCHANGE_STORAGE)
    : base(type)
        {
            Character = character;
            Storage = storage;
        }

        public override void Create()
        {
            if (Storage.ActualUser != -1)
            {
                Character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_STORAGE_ALREADY_IN_USE));
                Character.AddMessage(() => Character.StopAction(Action.GameActionTypeEnum.EXCHANGE));
                return;
            }

            Storage.ActualUser = Character.Id;
            Storage.AddHandler(Character.Dispatch);

            Character.CachedBuffer = true;
            base.Create();
            SendItemsList();
            Character.CachedBuffer = false;
        }

        public override void Leave(bool success = false)
        {
            if (Storage.ActualUser == Character.Id)
            {
                base.Leave(success);
                Storage.ActualUser = -1;
                Storage.RemoveHandler(Character.Dispatch);
            }
        }

        public void SendItemsList()
        {
            Character.Dispatch(WorldMessage.EXCHANGE_STORAGE_ITEMS_LIST(Storage.Items, Storage.Kamas));
        }

        public override long MoveKamas(AbstractEntity actor, long quantity)
        {
            Character.CachedBuffer = true;

            if (quantity < 0)
            {
                quantity = Math.Abs(quantity);
                if (quantity > Storage.Kamas)
                    quantity = Storage.Kamas;

                Storage.SubKamas(quantity);
                Character.Inventory.AddKamas(quantity);
            }
            else
            {
                if (quantity > Character.Inventory.Kamas)
                    quantity = Character.Inventory.Kamas;

                Storage.AddKamas(quantity);
                Character.Inventory.SubKamas(quantity);
            }
            Character.CachedBuffer = false;

            return quantity;
        }

        public override int AddItem(AbstractEntity actor, long guid, int quantity, long price = -1)
        {
            var item = Character.Inventory.RemoveItem(guid, quantity);
            if (item == null)
                return 0;

            Storage.AddItem(item);

            return item.Quantity;
        }

        public override int RemoveItem(AbstractEntity actor, long guid, int quantity)
        {
            var item = Storage.RemoveItem(guid, quantity);
            if (item == null)
                return 0;

            Character.Inventory.AddItem(item);

            return item.Quantity;
        }
    }
}


