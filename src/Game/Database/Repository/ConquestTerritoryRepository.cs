using Protocolo.Framework.Database;
using Game.Database.Structure;
using System.Collections.Generic;

namespace Game.Database.Repository
{
    public sealed class ConquestTerritoryRepository : Repository<ConquestTerritoryRepository, ConquestTerritoryDAO>
    {
        private readonly Dictionary<int, ConquestTerritoryDAO> m_bySubArea;

        public ConquestTerritoryRepository()
        {
            m_bySubArea = new Dictionary<int, ConquestTerritoryDAO>();
        }

        public override void Initialize(SqlManager sqlManager)
        {
            base.Initialize(sqlManager);
            foreach (var record in All)
                m_bySubArea[record.SubAreaId] = record;
        }

        public ConquestTerritoryDAO GetBySubArea(int subAreaId)
        {
            m_bySubArea.TryGetValue(subAreaId, out var record);
            return record;
        }

        public void Add(ConquestTerritoryDAO record)
        {
            m_bySubArea[record.SubAreaId] = record;
            base.Created(record);
        }

        public void Remove(ConquestTerritoryDAO record)
        {
            m_bySubArea.Remove(record.SubAreaId);
            base.Removed(record);
        }

        public void Update(ConquestTerritoryDAO record)
        {
            record.IsDirty = true;
        }
    }
}

