using Protocolo.Framework.Database;
using Game.Database.Structure;
using System.Collections.Generic;
using System.Linq;

namespace Game.Database.Repository
{
    public sealed class AccountGiftRepository : Repository<AccountGiftRepository, AccountGiftDAO>
    {
        private readonly Dictionary<long, List<AccountGiftDAO>> m_byAccount;

        public AccountGiftRepository()
        {
            m_byAccount = new Dictionary<long, List<AccountGiftDAO>>();
        }

        public override void Initialize(SqlManager sqlManager)
        {
            base.Initialize(sqlManager);
            foreach (var record in All)
                IndexRecord(record);
        }

        public List<AccountGiftDAO> GetByAccountId(long accountId)
        {
            m_byAccount.TryGetValue(accountId, out var list);
            return list ?? new List<AccountGiftDAO>();
        }

        public void Add(AccountGiftDAO record)
        {
            IndexRecord(record);
            base.Created(record);
        }

        public void Remove(AccountGiftDAO record)
        {
            if (m_byAccount.TryGetValue(record.AccountId, out var list))
                list.Remove(record);
            base.Removed(record);
        }

        private void IndexRecord(AccountGiftDAO record)
        {
            if (!m_byAccount.TryGetValue(record.AccountId, out var list))
            {
                list = new List<AccountGiftDAO>();
                m_byAccount[record.AccountId] = list;
            }
            list.Add(record);
        }
    }
}
