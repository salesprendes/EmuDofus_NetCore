using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Protocolo.Framework.Database;
using Login.Database.Structure;

namespace Login.Database.Repository
{
    public sealed class CharacterInstanceRepository : Repository<CharacterInstanceRepository, CharacterInstanceDAO>
    {
        private volatile Dictionary<long, Dictionary<int, int>> m_countsByAccountAndServer;

        public CharacterInstanceRepository() : base(loadOnly: true)
        {
            m_countsByAccountAndServer = new Dictionary<long, Dictionary<int, int>>();
        }

        public override void Initialize(SqlManager sqlMgr)
        {
            base.Initialize(sqlMgr);
            Reload();
        }

        public IReadOnlyDictionary<int, int> GetCountsByServer(long accountId)
        {
            var snapshot = m_countsByAccountAndServer;
            Dictionary<int, int> counts;
            return snapshot.TryGetValue(accountId, out counts) ? counts : _emptyDict;
        }

        public void Reload()
        {
            var newIndex = new Dictionary<long, Dictionary<int, int>>();

            foreach (var row in SqlMgr.Query<CharacterCountRow>("SELECT AccountId, ServerId, COUNT(*) AS Cnt FROM characterinstance GROUP BY AccountId, ServerId"))
            {
                Dictionary<int, int> inner;
                if (!newIndex.TryGetValue(row.AccountId, out inner))
                {
                    inner = new Dictionary<int, int>();
                    newIndex[row.AccountId] = inner;
                }
                inner[row.ServerId] = row.Cnt;
            }
            m_countsByAccountAndServer = newIndex;
        }

        private static readonly IReadOnlyDictionary<int, int> _emptyDict = FrozenDictionary<int, int>.Empty;

        private sealed class CharacterCountRow
        {
            public long AccountId { get; set; }
            public int ServerId { get; set; }
            public int Cnt { get; set; }
        }
    }
}
