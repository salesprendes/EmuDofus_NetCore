using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entity.Inventory;

namespace Game.Action
{
    public class GameStorageExchangeAction : AbstractGameExchangeAction
    {
        public GameStorageExchangeAction(CharacterEntity character, StorageInventory storage, ExchangeTypeEnum type = ExchangeTypeEnum.EXCHANGE_STORAGE)
    : base(new StorageExchange(character, storage, type), character, null)
        {
        }

        public override void Start()
        {
            Exchange.Create();
        }

        public override void Stop(params object[] args)
        {
            base.Leave(true);
            base.Stop(args);
        }

        public override void Abort(params object[] args)
        {
            base.Leave();
            base.Abort(args);
        }
    }
}


