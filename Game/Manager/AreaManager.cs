using System.Collections.Generic;
using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Area;
using System.Threading;

namespace Game.Manager
{
    public sealed class AreaManager : Singleton<AreaManager>
    {
        private readonly Dictionary<int, SuperAreaInstance> m_superAreaById;
        private readonly Dictionary<int, AreaInstance> m_areaById;
        private readonly Dictionary<int, SubAreaInstance> m_subAreaById;

        public IEnumerable<SuperAreaInstance> SuperAreas => m_superAreaById.Values;

        public IEnumerable<AreaInstance> Areas => m_areaById.Values;

        public IEnumerable<SubAreaInstance> SubAreas => m_subAreaById.Values;

        public AreaManager()
        {
            m_superAreaById = new Dictionary<int, SuperAreaInstance>();
            m_areaById = new Dictionary<int, AreaInstance>();
            m_subAreaById = new Dictionary<int, SubAreaInstance>();
        }

        public void Initialize()
        {
            foreach (var superAreaDAO in SuperAreaRepository.Instance.All)
            {
                var instance = new SuperAreaInstance(superAreaDAO);
                WorldService.Instance.AddUpdatable(instance);
                WorldService.Instance.Dispatcher.AddHandler(instance.SafeDispatch);

                m_superAreaById.Add(superAreaDAO.Id, instance);
            }

            foreach (var areaDAO in AreaRepository.Instance.All)
            {
                var instance = new AreaInstance(areaDAO);
                instance.SuperArea.AddUpdatable(instance);
                instance.SuperArea.AddHandler(instance.SafeDispatch);

                m_areaById.Add(areaDAO.Id, instance);
            }

            foreach (var subAreaDAO in SubAreaRepository.Instance.All)
            {
                var instance = new SubAreaInstance(subAreaDAO);
                instance.Area.AddHandler(instance.SafeDispatch);
                m_subAreaById.Add(subAreaDAO.Id, instance);
            }
        }

        public SuperAreaInstance GetSuperArea(int id)
        {
            return m_superAreaById[id];
        }

        public AreaInstance GetArea(int id)
        {
            return m_areaById[id];
        }

        public bool TryGetArea(int id, out AreaInstance area)
        {
            return m_areaById.TryGetValue(id, out area);
        }

        public SubAreaInstance GetSubArea(int id)
        {
            return m_subAreaById[id];
        }

        public bool TryGetSubArea(int id, out SubAreaInstance subArea)
        {
            return m_subAreaById.TryGetValue(id, out subArea);
        }
    }
}


