using System.Text;
using Protocolo.Framework.Database;
using System;
using Game.Database.Repository;
using Game.Stats;
using Game.Spell;
using Game.Entity;
using Game.Condition;

namespace Game.Database.Structure
{
    [Table("inventoryitem")]
    public sealed class ItemDAO : DataAccessObject<ItemDAO>
    {
        private long _id;
        private int _ownerType;
        private long _ownerId;
        private int _templateId;
        private int _slotId;
        private int _quantity;
        private string _stringEffects;
        private long _merchantPrice;
        private double _forgemagiePuits;
        private ItemTemplateDAO m_template;
        private GenericStats m_statistics;

        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int OwnerType
        {
            get => _ownerType;
            set => SetProperty(ref _ownerType, value);
        }

        public long OwnerId
        {
            get => _ownerId;
            set => SetProperty(ref _ownerId, value);
        }

        public int TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
        }

        public int SlotId
        {
            get => _slotId;
            set => SetProperty(ref _slotId, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public string StringEffects
        {
            get => _stringEffects;
            set => SetProperty(ref _stringEffects, value);
        }

        public long MerchantPrice
        {
            get => _merchantPrice;
            set => SetProperty(ref _merchantPrice, value);
        }

        public double ForjamagiaPozo
        {
            get => _forgemagiePuits;
            set => SetProperty(ref _forgemagiePuits, value);
        }

        [Write(false)]
        public ItemTemplateDAO Template
        {
            get
            {
                if (m_template == null)
                    m_template = ItemTemplateRepository.Instance.GetById(TemplateId);

                return m_template;
            }
        }

        [Write(false)]
        public GenericStats Statistics
        {
            get
            {
                if (m_statistics == null)
                    m_statistics = GenericStats.ParseFromString(StringEffects);

                return m_statistics;
            }
        }

        public void SaveStats()
        {
            Statistics.StatisticsChanged();
            StringEffects = Statistics.ToItemStats();
        }

        [Write(false)] public ItemSlotEnum Slot => (ItemSlotEnum)SlotId;

        [Write(false)] public bool IsEquiped => IsEquipedSlot(Slot);

        [Write(false)] public bool IsBoostEquiped => IsBoostSlot(Slot);

        [Write(false)] public bool IsEthereal => Template?.Ethereal ?? false;


        [Write(false)]
        public int Durability
        {
            get
            {
                if (!Statistics.HasEffect(EffectEnum.OBJETO_RESISTENCIA_ETEREA))
                    return -1;
                return Statistics.GetEffect(EffectEnum.OBJETO_RESISTENCIA_ETEREA).Value2;
            }
        }


        [Write(false)]
        public int MaxDurability
        {
            get
            {
                if (!Statistics.HasEffect(EffectEnum.OBJETO_RESISTENCIA_ETEREA))
                    return -1;
                return Statistics.GetEffect(EffectEnum.OBJETO_RESISTENCIA_ETEREA).Value3;
            }
        }

        public void DecreaseDurability()
        {
            if (!Statistics.HasEffect(EffectEnum.OBJETO_RESISTENCIA_ETEREA))
                return;
            var effect = Statistics.GetEffect(EffectEnum.OBJETO_RESISTENCIA_ETEREA);

            if (effect.Value3 <= 0 || effect.Value2 <= 0)
                return;
            effect.Value2--;
            SaveStats();
        }

        public bool SatisfyConditions(CharacterEntity character)
        {
            if (Template.Conditions == string.Empty)
                return true;
            return ConditionParser.Instance.Check(Template.Conditions, character);
        }

        public const int LivingExchangeLockMonths = 2;

        public static void SetDateEffect(GenericStats stats, EffectEnum effect, DateTime date)
        {
            var itemEffect = stats.GetEffect(effect);
            itemEffect.Value1 = date.Year;
            itemEffect.Value2 = ((date.Month - 1) * 100) + date.Day;
            itemEffect.Value3 = (date.Hour * 100) + date.Minute;
            itemEffect.Args = "0";
        }

        public static bool EnsureLivingReceptionStats(GenericStats stats, DateTime receivedAt)
        {
            var changed = false;
            var hadReceivedDate = stats.HasEffect(EffectEnum.OBJETO_RECIBIDO);

            if (!hadReceivedDate)
            {
                SetDateEffect(stats, EffectEnum.OBJETO_RECIBIDO, receivedAt);
                changed = true;


                if (stats.HasEffect(EffectEnum.OBJETO_VIVO_HUMOR))
                    stats.GetEffect(EffectEnum.OBJETO_VIVO_HUMOR).Value3 = 0;
            }

            if (!hadReceivedDate && !stats.HasEffect(EffectEnum.OBJETO_PUEDE_INTERCAMBIARSE))
            {
                SetDateEffect(stats, EffectEnum.OBJETO_PUEDE_INTERCAMBIARSE, receivedAt.AddMonths(LivingExchangeLockMonths));
                changed = true;
            }

            return changed;
        }

        public static DateTime? GetDateEffect(GenericStats stats, EffectEnum effect)
        {
            if (stats == null || !stats.HasEffect(effect))
                return null;

            var itemEffect = stats.GetEffect(effect);
            var month = (itemEffect.Value2 / 100) + 1;
            var day = itemEffect.Value2 % 100;
            var hour = itemEffect.Value3 / 100;
            var minute = itemEffect.Value3 % 100;

            if (itemEffect.Value1 < 1 || month < 1 || month > 12 || day < 1 || day > 31 || hour < 0 || hour > 23 || minute < 0 || minute > 59)
                return null;

            try
            {
                return new DateTime(itemEffect.Value1, month, day, hour, minute, 0);
            }
            catch
            {
                return null;
            }
        }

        public bool RefreshTemporaryExchangeLock(DateTime? currentTime = null)
        {
            var exchangeDate = GetDateEffect(Statistics, EffectEnum.OBJETO_PUEDE_INTERCAMBIARSE);
            if (exchangeDate == null || exchangeDate.Value > (currentTime ?? DateTime.Now))
                return false;

            if (!Statistics.RemoveEffect(EffectEnum.OBJETO_PUEDE_INTERCAMBIARSE))
                return false;

            SaveStats();
            return true;
        }

        public bool IsTemporarilyLockedFromExchange(DateTime? currentTime = null)
        {
            var exchangeDate = GetDateEffect(Statistics, EffectEnum.OBJETO_PUEDE_INTERCAMBIARSE);
            return exchangeDate == null ? Statistics.HasEffect(EffectEnum.OBJETO_PUEDE_INTERCAMBIARSE) : exchangeDate.Value > (currentTime ?? DateTime.Now);
        }

        public static bool IsEquipedSlot(ItemSlotEnum slot)
        {
            return slot >= ItemSlotEnum.SLOT_AMULET && slot <= ItemSlotEnum.SLOT_BOOST_FOLLOWER;
        }

        public static bool IsBoostSlot(ItemSlotEnum slot)
        {
            return slot >= ItemSlotEnum.SLOT_BOOST_MUTATION && slot <= ItemSlotEnum.SLOT_BOOST_FOLLOWER;
        }

        public void SerializeAs_BagContent(StringBuilder message)
        {
            message
                .Append(Id.ToString("x")).Append('~')
                .Append(TemplateId.ToString("x")).Append('~')
                .Append(Quantity.ToString("x")).Append('~')
                .Append((SlotId != (int)ItemSlotEnum.SLOT_INVENTORY ? SlotId.ToString("x") : "")).Append('~')
                .Append(StringEffects).Append(';');
        }

        public override string ToString()
        {
            return (Id.ToString("x")) + ('~') +
                   (TemplateId.ToString("x")) + ('~') +
                   (Quantity.ToString("x")) + ('~') +
                   ((SlotId != (int)ItemSlotEnum.SLOT_INVENTORY ? SlotId.ToString("x") : "")) + ('~') +
                   (StringEffects) + (';');
        }

        public string ToExchangeString()
        {
            return Id.ToString() + "|" + Quantity + "|" + TemplateId + "|" + StringEffects;
        }

        public ItemDAO Clone(int quantity)
        {
            return InventoryItemRepository.Instance.Create(TemplateId, OwnerId, OwnerType, quantity, Statistics, ItemSlotEnum.SLOT_INVENTORY);
        }
    }
}
