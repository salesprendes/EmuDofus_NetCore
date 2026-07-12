using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public sealed class TaxCollectorExchange : StorageExchange
    {
        public TaxCollectorEntity TaxCollector
        {
            get;
            private set;
        }

        public TaxCollectorExchange(CharacterEntity character, TaxCollectorEntity taxCollector)
    : base(character, taxCollector.Storage, ExchangeTypeEnum.EXCHANGE_TAXCOLLECTOR)
        {
            TaxCollector = taxCollector;
        }

        protected override string SerializeAs_ExchangeCreate()
        {
            return TaxCollector.Id.ToString();
        }

        public override void Leave(bool success = false)
        {
            base.Leave(success);
            // El jugador puede haber sido expulsado del gremio con el intercambio abierto.
            Character.GuildMember?.FarmTaxCollector(TaxCollector);
        }

        public override long MoveKamas(AbstractEntity actor, long quantity)
        {

            if (quantity > 0)
                return 0;

            return base.MoveKamas(actor, quantity);
        }

        public override int AddItem(AbstractEntity actor, long guid, int quantity, long price = -1)
        {
            return 0;
        }

        public override int RemoveItem(AbstractEntity actor, long guid, int quantity)
        {
            var item = Storage.GetItem(guid);
            if (item == null)
                return 0;

            var templateId = item.TemplateId;

            quantity = base.RemoveItem(actor, guid, quantity);

            if (quantity > 0)
            {
                if (!TaxCollector.FarmedItems.ContainsKey(templateId))
                    TaxCollector.FarmedItems.Add(templateId, 0);
                TaxCollector.FarmedItems[templateId] += quantity;
            }
            return quantity;
        }
    }
}


