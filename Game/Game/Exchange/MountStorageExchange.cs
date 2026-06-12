using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public sealed class MountStorageExchange : AbstractExchange
    {
        public MountStorageExchange()
    : base(ExchangeTypeEnum.EXCHANGE_MOUNT_STORAGE)
        {
        }

        protected override string SerializeAs_ExchangeCreate()
        {

            return base.SerializeAs_ExchangeCreate();
        }
    }
}


