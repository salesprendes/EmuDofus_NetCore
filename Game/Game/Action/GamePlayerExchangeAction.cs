using Game.Entity;
using Game.Exchange;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GamePlayerExchangeAction : AbstractGameExchangeAction
    {
        public GamePlayerExchangeAction(CharacterEntity localEntity, CharacterEntity distantEntity)
    : base(new PlayerExchange(localEntity, distantEntity), localEntity, distantEntity)
        {
            Exchange.Dispatch(WorldMessage.EXCHANGE_REQUEST(Entity.Id, DistantEntity.Id));
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


