using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entity.Inventory
{
    public sealed class BankInventory : StorageInventory
    {
        public override long Kamas
        {
            get
            {
                return m_record.Kamas;
            }
            set
            {
                m_record.Kamas = value;
            }
        }

        private readonly BankDAO m_record;

        public BankInventory(BankDAO databaseRecord)
    : base((int)EntityTypeEnum.TYPE_BANK, databaseRecord.Id)
        {
            m_record = databaseRecord;
        }
    }
}


