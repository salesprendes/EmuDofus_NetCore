using System.Collections.Generic;
using Protocolo.Framework.Database;
using Login.Database.Structure;

namespace Login.Database.Repository
{
    public sealed class AccountRepository : Repository<AccountRepository, AccountDAO>
    {
        private Dictionary<long, AccountDAO> m_accountById;

        private Dictionary<string, AccountDAO> m_accountByName;

        public AccountRepository()
        {
            m_accountById = new Dictionary<long, AccountDAO>();
            m_accountByName = new Dictionary<string, AccountDAO>();
        }

        public AccountDAO GetById(long accountId)
        {
            AccountDAO account = null;
            m_accountById.TryGetValue(accountId, out account);
            return account;
        }

        public AccountDAO GetByName(string accountName)
        {
            AccountDAO account = null;
            if (!m_accountByName.TryGetValue(accountName.ToLower(), out account))
                account = Load("upper(name)=upper(@name)", new { name = accountName });
            return account;
        }

        public override void OnObjectAdded(AccountDAO account)
        {
            m_accountById.Add(account.Id, account);
            m_accountByName.Add(account.Name.ToLower(), account);
        }

        public override void OnObjectRemoved(AccountDAO account)
        {
            m_accountById.Remove(account.Id);
            m_accountByName.Remove(account.Name.ToLower());
        }
    }
}

