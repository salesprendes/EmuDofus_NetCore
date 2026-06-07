using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.House;
using System.Collections.Generic;

namespace Game.Manager
{
    public sealed class HouseManager : Singleton<HouseManager>
    {
        private readonly Dictionary<int, HouseInstance> m_houseById;
        private readonly Dictionary<int, HouseInstance> m_houseByInsideMapId;
        private readonly Dictionary<int, List<HouseInstance>> m_housesByOutsideMapId;

        public HouseManager()
        {
            m_houseById = new Dictionary<int, HouseInstance>();
            m_houseByInsideMapId = new Dictionary<int, HouseInstance>();
            m_housesByOutsideMapId = new Dictionary<int, List<HouseInstance>>();
        }

        public void Initialize()
        {
            var count = 0;
            foreach (var record in HouseRepository.Instance.All)
            {
                var house = new HouseInstance(record);
                m_houseById[house.Id] = house;
                m_houseByInsideMapId[house.MapIdInside] = house;
                if (!m_housesByOutsideMapId.TryGetValue(house.MapIdOutside, out var list))
                {
                    list = new List<HouseInstance>();
                    m_housesByOutsideMapId[house.MapIdOutside] = list;
                }
                list.Add(house);
                count++;
            }
            Logger.Info("HouseManager: " + count + " casas cargadas.");
        }

        public HouseInstance GetById(int id)
        {
            m_houseById.TryGetValue(id, out var house);
            return house;
        }

        public HouseInstance GetByInsideMapId(int mapId)
        {
            m_houseByInsideMapId.TryGetValue(mapId, out var house);
            return house;
        }

        public List<HouseInstance> GetAllByOutsideMapId(int mapId)
        {
            m_housesByOutsideMapId.TryGetValue(mapId, out var list);
            return list;
        }
    }
}
