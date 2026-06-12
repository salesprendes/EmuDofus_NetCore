using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class TaxCollectorRepository : Repository<TaxCollectorRepository, TaxCollectorDAO>
    {
        public long NextTaxCollectorId
        {
            get
            {
                lock (m_syncLock)
                    return m_nextTaxCollectorId++;
            }
        }

        private long m_nextTaxCollectorId;

        public TaxCollectorRepository()
        {
            m_nextTaxCollectorId = 1000;
        }

        public override void OnObjectAdded(TaxCollectorDAO obj)
        {
            if (obj.Id >= m_nextTaxCollectorId)
                m_nextTaxCollectorId = obj.Id + 1;
        }
    }
}

