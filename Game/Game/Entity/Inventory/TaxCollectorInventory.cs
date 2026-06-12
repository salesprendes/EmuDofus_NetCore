using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entity.Inventory
{
    public sealed class TaxCollectorInventory : StorageInventory
    {
        public override long Kamas
        {
            get
            {
                return m_taxCollector.Kamas;
            }
            set
            {
                m_taxCollector.Kamas = value;
            }
        }

        private readonly TaxCollectorEntity m_taxCollector;

        public TaxCollectorInventory(TaxCollectorEntity taxCollecor)
    : base((int)EntityTypeEnum.TYPE_TAX_COLLECTOR, taxCollecor.Id)
        {
            m_taxCollector = taxCollecor;
        }
    }
}


