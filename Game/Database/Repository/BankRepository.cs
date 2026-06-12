using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class BankRepository : Repository<BankRepository, BankDAO>
    {
        private Dictionary<long, BankDAO> m_bankByAccountId;

        public BankRepository()
        {
            m_bankByAccountId = new Dictionary<long, BankDAO>();
        }

        public override void OnObjectAdded(BankDAO bank)
        {
            m_bankByAccountId.Add(bank.Id, bank);
        }

        public BankDAO GetByAccountId(long accountId)
        {
            if (m_bankByAccountId.ContainsKey(accountId))
                return m_bankByAccountId[accountId];
            var bank = new BankDAO() { Id = accountId };
            base.Created(bank);
            return bank;
        }
    }
}

