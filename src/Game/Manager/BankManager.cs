using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entity.Inventory;

namespace Game.Manager
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BankManager : Singleton<BankManager>
    {

        /// <summary>
        /// 
        /// </summary>
        private readonly Dictionary<long, BankInventory> m_bankByAccountId;
        
        /// <summary>
        /// 
        /// </summary>
        public BankManager()
        {
            m_bankByAccountId = new Dictionary<long, BankInventory>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
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


