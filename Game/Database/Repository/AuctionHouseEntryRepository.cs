using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class AuctionHouseEntryRepository : Repository<AuctionHouseEntryRepository, AuctionHouseEntryDAO>
    {
        public AuctionHouseEntryDAO Create(long itemId, int houseId, long ownerId, long price, long time)
        {
            var entry = new AuctionHouseEntryDAO() { ItemId = itemId, AuctionHouseId = houseId, OwnerId = ownerId, Price = price, ExpireDate = DateTime.Now.AddHours((double)time), };
            base.Created(entry);
            return entry;
        }
    }
}

