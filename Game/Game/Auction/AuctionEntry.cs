using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity;
using Game.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entity.Inventory;

namespace Game.Auction
{
    public sealed class AuctionEntry : IComparable<AuctionEntry>
    {
        public long ItemId => m_databaseRecord.ItemId;

        public int AuctionHouseId => m_databaseRecord.AuctionHouseId;

        public long OwnerId => m_databaseRecord.OwnerId;

        public long Price => m_databaseRecord.Price;

        public DateTime ExpireDate => m_databaseRecord.ExpireDate;

        public int HoursLeft => (int)Math.Floor(ExpireDate.Subtract(DateTime.Now).TotalHours);

        public ItemDAO Item
        {
            get
            {
                if (m_item == null)
                    m_item = InventoryItemRepository.Instance.GetById(ItemId);
                return m_item;
            }
        }

        public BankInventory OwnerBank
        {
            get
            {
                if (m_owner == null)
                    m_owner = BankManager.Instance.GetBankByAccountId(OwnerId);
                return m_owner;
            }
        }

        private AuctionHouseEntryDAO m_databaseRecord;

        private ItemDAO m_item;

        private BankInventory m_owner;

        public AuctionEntry(AuctionHouseEntryDAO record, ItemDAO item = null)
        {
            m_databaseRecord = record;
            m_item = item;
        }

        public void Remove()
        {
            AuctionHouseEntryRepository.Instance.Removed(m_databaseRecord);
        }

        public int CompareTo(AuctionEntry other)
        {
            if (Price < other.Price)
                return -1;
            if (Price > other.Price)
                return 1;
            return 0;
        }
    }
}


