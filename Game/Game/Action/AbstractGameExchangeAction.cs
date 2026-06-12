using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public abstract class AbstractGameExchangeAction : AbstractGameAction
    {
        public AbstractExchange Exchange
        {
            get;
            private set;
        }

        public AbstractEntity DistantEntity
        {
            get;
            private set;
        }

        public override bool CanAbort => true;

        public AbstractGameExchangeAction(AbstractExchange exchange, AbstractEntity localEntity, AbstractEntity distantEntity = null)
    : base(GameActionTypeEnum.EXCHANGE, localEntity)
        {
            DistantEntity = distantEntity;
            Exchange = exchange;
            Exchange.AddHandler(Entity.Dispatch);
            if (DistantEntity != null)
                Exchange.AddHandler(DistantEntity.Dispatch);
            Entity.AddUpdatable(Exchange);
        }

        public void Accept()
        {
            Exchange.Create();
        }

        public void Leave(bool success = false)
        {
            Exchange.Leave(success);
            Exchange.RemoveHandler(Entity.Dispatch);
            if (DistantEntity != null)
                Exchange.RemoveHandler(DistantEntity.Dispatch);
            Entity.RemoveUpdatable(Exchange);
        }
    }
}


