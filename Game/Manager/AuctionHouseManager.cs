using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Auction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Manager
{
    public sealed class AuctionHouseManager : Singleton<AuctionHouseManager>
    {
        private readonly Dictionary<int, AuctionHouseInstance> m_auctionHousesById;
        private readonly Dictionary<int, AuctionHouseInstance> m_auctionHouseByNpcId;

        public AuctionHouseManager()
        {
            m_auctionHousesById = new Dictionary<int, AuctionHouseInstance>();
            m_auctionHouseByNpcId = new Dictionary<int, AuctionHouseInstance>();
        }

        public void Initialize()
        {
            foreach (var auctionHouseDao in AuctionHouseRepository.Instance.All)
            {
                var auctionHouse = new AuctionHouseInstance(auctionHouseDao);
                m_auctionHousesById.Add(auctionHouseDao.Id, auctionHouse);
                m_auctionHouseByNpcId.Add(auctionHouseDao.NpcId, auctionHouse);
            }

            foreach (var auctionHouseAllowedTypeDao in AuctionHouseAllowedTypeRepository.Instance.All)
                m_auctionHousesById[auctionHouseAllowedTypeDao.AuctionHouseId].AddAllowedType(auctionHouseAllowedTypeDao.TemplateId);

            foreach (var auctionHouseEntry in AuctionHouseEntryRepository.Instance.All)
                m_auctionHousesById[auctionHouseEntry.AuctionHouseId].Add(new AuctionEntry(auctionHouseEntry));
        }

        public AuctionHouseInstance GetByNpcId(int npcId)
        {
            if (m_auctionHouseByNpcId.ContainsKey(npcId))
                return m_auctionHouseByNpcId[npcId];
            return null;
        }
    }
}


