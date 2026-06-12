using Game.Database.Structure;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entity.Inventory
{
    public class StorageInventory : PersistentInventory
    {
        public long ActualUser
        {
            get;
            set;
        }

        public StorageInventory(int ownerType = (int)EntityTypeEnum.TYPE_STORAGE, long ownerId = -1)
    : base(ownerType, ownerId)
        {
            ActualUser = -1;
        }

        public override void OnItemAdded(ItemDAO item)
        {
            Dispatch(WorldMessage.EXCHANGE_STORAGE_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, item.ToExchangeString()));
        }

        public override void OnItemQuantity(long itemId, int quantity)
        {
            OnItemAdded(GetItem(itemId));
        }

        public override void OnItemRemoved(long itemId)
        {
            Dispatch(WorldMessage.EXCHANGE_STORAGE_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_REMOVE, itemId.ToString()));
        }

        public override void OnKamasAdded(long value)
        {
            Dispatch(WorldMessage.EXCHANGE_STORAGE_KAMAS_VALUE(Kamas));
        }

        public override void OnKamasSubstracted(long value)
        {
            Dispatch(WorldMessage.EXCHANGE_STORAGE_KAMAS_VALUE(Kamas));
        }
    }
}


