using Game.Database.Structure;
using System;
using System.Collections.Generic;
using Game.Manager;
using Game.Network;
using Protocolo.Framework.Generic;
using Game.Entity;
using Game.Database.Repository;

namespace Game.Area
{
    public sealed class SubAreaInstance : MessageDispatcher
    {
        private SubAreaDAO m_subAreaRecord;
        private AreaInstance m_area;
        private IEnumerable<MonsterSpawnDAO> m_spawns;


        public IEnumerable<MonsterSpawnDAO> Spawns
        {
            get
            {
                if (m_spawns == null)
                    m_spawns = MonsterSpawnRepository.Instance.GetById(ZoneTypeEnum.TYPE_SUBAREA, m_subAreaRecord.Id);
                return m_spawns;
            }
        }

        public int Id => m_subAreaRecord.Id;
        public int AreaId => m_subAreaRecord.AreaId;
        public string Name => m_subAreaRecord.Name;
        public bool CanConquest => m_subAreaRecord.CanConquest != 0;
        public int DefaultAlignment => m_subAreaRecord.DefaultAlignment;
        public bool PremiumZone => m_subAreaRecord.PremiumZone != 0;


        public AreaInstance Area
        {
            get
            {
                if (m_area == null)
                    m_area = AreaManager.Instance.GetArea(m_subAreaRecord.AreaId);
                return m_area;
            }
        }

        private static readonly BasicTaskProcessor s_sharedQueue = new BasicTaskProcessor("SubAreas", 30);


        public TaxCollectorEntity TaxCollector
        {
            get;
            set;
        }

        public SubAreaInstance(SubAreaDAO record)
        {
            m_subAreaRecord = record;
            s_sharedQueue.AddUpdatable(this);
        }

        public new void AddHandler(Action<string> method)
        {
            s_sharedQueue.AddMessage(() => base.AddHandler(method));
        }

        public override void RemoveHandler(Action<string> method)
        {
            s_sharedQueue.AddMessage(() => base.RemoveHandler(method));
        }

        public override void Dispatch(string message)
        {
            s_sharedQueue.AddMessage(() => base.Dispatch(message));
        }
    }
}


