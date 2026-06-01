using Protocolo.Framework.Generic;
using Game.Database.Repository;
using System.Collections.Generic;
using Game.Entity.Inventory;

namespace Game.Manager
{
    public sealed class BankManager : Singleton<BankManager>
    {
        private readonly Dictionary<long, BankInventory> m_bankByAccountId;
        
        public BankManager()
        {
            m_bankByAccountId = new Dictionary<long, BankInventory>();
        }

        public BankInventory GetBankByAccountId(long accountId)
        {
            if(!m_bankByAccountId.ContainsKey(accountId))
            {
                var bank = new BankInventory(BankRepository.Instance.GetByAccountId(accountId));
                m_bankByAccountId.Add(accountId, bank);
                return bank;
            }
            return m_bankByAccountId[accountId];
        }
    }
}


