using Protocolo.Framework.Database;
using Game.Database.Structure;
using System.Collections.Generic;

namespace Game.Database.Repository
{
    public sealed class HouseRepository : Repository<HouseRepository, HouseDAO>
    {
        private readonly Dictionary<int, HouseDAO> m_houseById;
        private readonly Dictionary<int, int> m_houseByInsideMapId;

        public HouseRepository()
        {
            m_houseById = new Dictionary<int, HouseDAO>();
            m_houseByInsideMapId = new Dictionary<int, int>();
        }

        public override void OnObjectAdded(HouseDAO house)
        {
            m_houseById[house.Id] = house;
            m_houseByInsideMapId[house.MapIdInside] = house.Id;
        }

        public HouseDAO GetById(int id)
        {
            m_houseById.TryGetValue(id, out var house);
            return house;
        }

        public HouseDAO GetByInsideMapId(int mapId)
        {
            if (m_houseByInsideMapId.TryGetValue(mapId, out var id))
                return GetById(id);
            return null;
        }
    }
}
