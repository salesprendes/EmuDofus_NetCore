using Protocolo.Framework.Database;
using Game.Database.Structure;
using Game.Stats;
using System.Collections.Generic;
using System.Linq;

namespace Game.Database.Repository
{
    /// <summary>
    ///
    /// </summary>
    public sealed class InventoryItemRepository : Repository<InventoryItemRepository, ItemDAO>
    {
        /// <summary>
        ///
        /// </summary>
        public long NextItemId
        {
            get
            {
                lock (m_syncLock)
                    return m_nextItemId++;
            }
        }

        private long m_nextItemId;
        private Dictionary<long, ItemDAO> m_itemById;
        private Dictionary<long, List<ItemDAO>> m_itemsByOwner;

        public InventoryItemRepository()
        {
            m_itemById = new Dictionary<long, ItemDAO>();
            m_itemsByOwner = new Dictionary<long, List<ItemDAO>>();
        }

        private static long OwnerKey(int ownerType, long ownerId) => ((long)ownerType << 48) | (ownerId & 0x0000FFFFFFFFFFFFL);

        public override void OnObjectAdded(ItemDAO item)
        {
            if (item.Id >= m_nextItemId)
                m_nextItemId = item.Id + 1;

            m_itemById[item.Id] = item;

            var key = OwnerKey(item.OwnerType, item.OwnerId);
            if (!m_itemsByOwner.TryGetValue(key, out var list))
            {
                list = new List<ItemDAO>();
                m_itemsByOwner[key] = list;
            }
            list.Add(item);
        }

        public override void OnObjectRemoved(ItemDAO item)
        {
            m_itemById.Remove(item.Id);

            var key = OwnerKey(item.OwnerType, item.OwnerId);
            if (m_itemsByOwner.TryGetValue(key, out var list))
            {
                list.Remove(item);
                if (list.Count == 0)
                    m_itemsByOwner.Remove(key);
            }
        }

        public ItemDAO GetById(long itemId)
        {
            m_itemById.TryGetValue(itemId, out var item);
            return item;
        }

        public IEnumerable<ItemDAO> GetByOwner(int ownerType, long ownerId)
        {
            var key = OwnerKey(ownerType, ownerId);
            if (m_itemsByOwner.TryGetValue(key, out var list))
                return list;
            return Enumerable.Empty<ItemDAO>();
        }

        public void EntityRemoved(int type, long id)
        {
            var key = OwnerKey(type, id);
            if (!m_itemsByOwner.TryGetValue(key, out var list))
                return;
            base.Removed(list.ToArray());
        }

        public override void InsertAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
            lock (m_syncLock)
                m_dataObjects.RemoveAll(item => item.IsNew && item.OwnerId == -1);

            base.InsertAll(connection, transaction);
        }

        public override void DeleteAll(MySqlConnector.MySqlConnection connection, MySqlConnector.MySqlTransaction transaction)
        {
            lock (m_syncLock)
                foreach (var item in m_dataObjects)
                    if (item.OwnerId == -1)
                        item.IsDeleted = true;

            base.DeleteAll(connection, transaction);
        }

        public ItemDAO Create(int templateId, long ownerId, int ownerType, int quantity, GenericStats statistics, ItemSlotEnum slot = ItemSlotEnum.SLOT_INVENTORY)
        {
            ItemDAO instance = new ItemDAO();
            instance.Id = NextItemId;
            instance.OwnerId = ownerId;
            instance.OwnerType = ownerType;
            instance.TemplateId = templateId;
            instance.Quantity = quantity;
            instance.StringEffects = statistics.ToItemStats();
            instance.SlotId = (int)slot;

            base.Created(instance);

            return instance;
        }
    }
}
