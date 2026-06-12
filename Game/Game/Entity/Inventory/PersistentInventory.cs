using Game.Database.Repository;
using Game.Database.Structure;
using System.Collections.Generic;

namespace Game.Entity.Inventory
{
    public class PersistentInventory : AbstractInventory
    {
        public int OwnerType
        {
            get;
        }

        public long OwnerId
        {
            get;
        }

        public override long Kamas
        {
            get;
            set;
        }

        public override List<ItemDAO> Items => m_items;

        private readonly List<ItemDAO> m_items;

        public PersistentInventory(int ownerType, long ownerId)
        {
            m_items = new List<ItemDAO>();
            m_items.AddRange(InventoryItemRepository.Instance.GetByOwner(ownerType, ownerId));

            OwnerType = ownerType;
            OwnerId = ownerId;
        }

        public override void OnOwnerChange(ItemDAO item)
        {
            item.OwnerId = OwnerId;
            item.OwnerType = OwnerType;
            InventoryItemRepository.Instance.AddOwnerReference(item);
        }
    }
}


