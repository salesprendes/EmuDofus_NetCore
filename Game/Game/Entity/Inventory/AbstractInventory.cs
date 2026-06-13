using Game.Database.Structure;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Entity.Inventory
{
    public abstract class AbstractInventory : MessageDispatcher
    {
        public abstract long Kamas
        {
            get;
            set;
        }

        public abstract List<ItemDAO> Items
        {
            get;
        }

        public virtual void OnKamasAdded(long value)
        {
        }

        public virtual void OnKamasSubstracted(long value)
        {
        }

        public virtual void OnItemAdded(ItemDAO item)
        {
        }

        public virtual void OnOwnerChange(ItemDAO item)
        {
        }

        public virtual void OnItemQuantity(long itemId, int quantity)
        {
        }

        public virtual void OnItemRemoved(long itemId)
        {
        }

        public void AddKamas(long value)
        {
            if (value < 0)
                throw new ArgumentException($"InventoryBag::AddKamas el valor debe ser mayor que 0: {value}");
            Kamas += value;
            OnKamasAdded(value);
        }

        public void SubKamas(long value)
        {
            if (value < 0)
                throw new ArgumentException($"InventoryBag::SubKamas el valor debe ser mayor que 0: {value}");
            Kamas -= value;
            OnKamasSubstracted(value);
        }

        protected virtual bool CheckWeight(ItemDAO item) => true;

        public bool AddItem(ItemDAO item, bool merge = true)
        {
            if (Items.Contains(item))
                return false;

            if (!CheckWeight(item))
                return false;

            if (merge)
                if (TryMerge(item))
                    return true;

            Items.Add(item);
            OnItemAdded(item);
            OnOwnerChange(item);

            return false;
        }

        public bool TryMerge(ItemDAO item)
        {
            var sameItem = Items.Find(
                entry => entry.TemplateId == item.TemplateId &&
                    entry.StringEffects == item.StringEffects &&
                    entry.Id != item.Id &&
                    entry.SlotId == item.SlotId &&
                    !ItemDAO.IsEquipedSlot(entry.Slot));

            if (sameItem != null)
            {
                sameItem.Quantity += item.Quantity;
                OnItemQuantity(sameItem.Id, sameItem.Quantity);
                return true;
            }

            return false;
        }

        public ItemDAO MoveQuantity(ItemDAO item, int quantity, ItemSlotEnum slot = ItemSlotEnum.SLOT_INVENTORY)
        {
            if (quantity >= item.Quantity)
                return RemoveItem(item.Id, item.Quantity);
            item.Quantity -= quantity;
            OnItemQuantity(item.Id, item.Quantity);
            return item.Clone(quantity);
        }

        public bool IsEquipedOf(long guid, int templateId)
        {
            return Items.Any(item => item.Id != guid && item.IsEquiped && item.TemplateId == templateId);
        }

        public bool HasTemplate(int templateId)
        {
            return Items.Any(item => item.TemplateId == templateId);
        }

        public bool NotHasTemplate(int templateId)
        {
            return !HasTemplate(templateId);
        }

        public bool HasTemplateEquiped(int templateId)
        {
            return Items.Any(item => item.TemplateId == templateId && item.IsEquiped);
        }


        public virtual IEnumerable<ItemDAO> RemoveItems()
        {
            foreach (var item in Items.ToArray())
            {
                Items.Remove(item);
                OnItemRemoved(item.Id);
                item.OwnerId = -1;
                yield return item;
            }
        }

        public virtual ItemDAO RemoveItem(long itemId, int quantity = 1)
        {
            var item = Items.Find(entry => entry.Id == itemId);
            if (item == null)
                return null;

            if (quantity >= item.Quantity)
            {
                Items.Remove(item);
                OnItemRemoved(item.Id);
            }
            else
            {
                item = MoveQuantity(item, quantity);
            }

            item.OwnerId = -1;

            return item;
        }

        public ItemDAO GetItem(long id)
        {
            return Items.Find(item => item.Id == id);
        }

        public void SerializeAs_BagContent(StringBuilder message)
        {
            foreach (var item in Items)
                item.SerializeAs_BagContent(message);
        }

        public override void Dispose()
        {
            Items.Clear();
            base.Dispose();
        }
    }
}


