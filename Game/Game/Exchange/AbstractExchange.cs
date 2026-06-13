using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public enum ExchangeTypeEnum
    {
        EXCHANGE_SHOP = 0,
        EXCHANGE_PLAYER = 1,
        EXCHANGE_NPC = 2,
        EXCHANGE_CRAFTPLAN = 3,
        EXCHANGE_MERCHANT = 4,
        EXCHANGE_STORAGE = 5,
        EXCHANGE_TAXCOLLECTOR = 8,
        EXCHANGE_PERSONAL_SHOP_EDIT = 6,
        EXCHANGE_AUCTION_HOUSE_SELL = 10,
        EXCHANGE_AUCTION_HOUSE_BUY = 11,
        EXCHANGE_CRAFT_SECURE_ARTISAN = 12, // craft seguro: el que aporta el oficio
        EXCHANGE_CRAFT_SECURE_CLIENT = 13,  // craft seguro: el que recibe el objeto
        EXCHANGE_MOUNT_STORAGE = 16,
        EXCHANGE_MOUNT = 15
    }

    public abstract class AbstractExchange : MessageDispatcher
    {
        public ExchangeTypeEnum Type
        {
            get;
            private set;
        }

        protected AbstractExchange(ExchangeTypeEnum type)
        {
            Type = type;
        }

        public virtual void Create()
        {
            base.Dispatch(WorldMessage.EXCHANGE_CREATE(Type, SerializeAs_ExchangeCreate()));
        }

        public virtual void Leave(bool success = false)
        {
            base.Dispatch(WorldMessage.EXCHANGE_LEAVE(success));
        }

        protected virtual string SerializeAs_ExchangeCreate()
        {
            return "";
        }

        public virtual int AddItem(AbstractEntity actor, long guid, int quantity, long price = -1)
        {
            return 0;
        }

        public virtual int RemoveItem(AbstractEntity actor, long guid, int quantity)
        {
            return 0;
        }

        public virtual long MoveKamas(AbstractEntity actor, long quantity)
        {
            return 0;
        }

        public virtual void BuyItem(AbstractEntity actor, long id, int quantity)
        {
        }

        public virtual void SellItem(AbstractEntity actor, long id, int quantity, long price = -1)
        {
        }
    }
}


