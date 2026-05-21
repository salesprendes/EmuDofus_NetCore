using System.Text;
using Protocolo.Framework.Database;
using System;
using Game.Database.Repository;
using Game.Stats;
using PropertyChanged;
using Game.Spell;
using Game.Entity;
using Game.Condition;

namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("inventoryitem")]
    [AddINotifyPropertyChangedInterface]
    public sealed class ItemDAO : DataAccessObject<ItemDAO>
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long Id
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int OwnerType
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long OwnerId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int TemplateId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int SlotId
        {
            get;
            set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public int Quantity
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string StringEffects
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long MerchantPrice
        {
            get;
            set;
        }
              
        /// <summary>
        /// 
        /// </summary>
        private ItemTemplateDAO m_template;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        [DoNotNotify]
        public ItemTemplateDAO Template
        {
            get
            {
                if (m_template == null)
                    m_template = ItemTemplateRepository.Instance.GetById(TemplateId);
                return m_template;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        private GenericStats m_statistics;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        [DoNotNotify]
        public GenericStats Statistics
        {
            get
            {
                if (m_statistics == null)
                    m_statistics = GenericStats.ParseFromString(StringEffects);
                return m_statistics;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="value"></param>
        public void SaveStats()
        {
            Statistics.StatisticsChanged();
            StringEffects = Statistics.ToItemStats();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Write(false)]
        [DoNotNotify]
        public ItemSlotEnum Slot => (ItemSlotEnum)SlotId;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>        
        [Write(false)]
        [DoNotNotify]
        public bool IsEquiped => IsEquipedSlot(Slot);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>        
        [Write(false)]
        [DoNotNotify]
        public bool IsBoostEquiped => IsBoostSlot(Slot);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
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
            var hadReceivedDate = stats.HasEffect(EffectEnum.Received);

            if (!hadReceivedDate)
            {
                SetDateEffect(stats, EffectEnum.Received, receivedAt);
                changed = true;

                // New items must start LEAN (hungry), regardless of what the DB template says
                if (stats.HasEffect(EffectEnum.LivingMood))
                    stats.GetEffect(EffectEnum.LivingMood).Value3 = 0;
            }

            if (!hadReceivedDate && !stats.HasEffect(EffectEnum.CanBeExchange))
            {
                SetDateEffect(stats, EffectEnum.CanBeExchange, receivedAt.AddMonths(LivingExchangeLockMonths));
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
            var exchangeDate = GetDateEffect(Statistics, EffectEnum.CanBeExchange);
            if (exchangeDate == null || exchangeDate.Value > (currentTime ?? DateTime.Now))
                return false;

            if (!Statistics.RemoveEffect(EffectEnum.CanBeExchange))
                return false;

            SaveStats();
            return true;
        }

        public bool IsTemporarilyLockedFromExchange(DateTime? currentTime = null)
        {
            var exchangeDate = GetDateEffect(Statistics, EffectEnum.CanBeExchange);
            return exchangeDate == null
                ? Statistics.HasEffect(EffectEnum.CanBeExchange)
                : exchangeDate.Value > (currentTime ?? DateTime.Now);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static bool IsEquipedSlot(ItemSlotEnum slot)
        {
            return slot >= ItemSlotEnum.SLOT_AMULET && slot <= ItemSlotEnum.SLOT_BOOST_FOLLOWER;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static bool IsBoostSlot(ItemSlotEnum slot)
        {
            return slot >= ItemSlotEnum.SLOT_BOOST_MUTATION && slot <= ItemSlotEnum.SLOT_BOOST_FOLLOWER;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void SerializeAs_BagContent(StringBuilder message)
        {
            message
                .Append(Id.ToString("x")).Append('~')
                .Append(TemplateId.ToString("x")).Append('~')
                .Append(Quantity.ToString("x")).Append('~')
                .Append((SlotId != (int)ItemSlotEnum.SLOT_INVENTORY ? SlotId.ToString("x") : "")).Append('~')
                .Append(StringEffects).Append(';');        
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return  (Id.ToString("x")) + ('~') +
                   (TemplateId.ToString("x")) + ('~') +
                   (Quantity.ToString("x")) +('~') +
                   ((SlotId != (int)ItemSlotEnum.SLOT_INVENTORY ? SlotId.ToString("x") : "")) + ('~') +
                   (StringEffects) + (';'); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ToExchangeString()
        {
            return Id.ToString() + "|" + Quantity + "|" + TemplateId + "|" + StringEffects;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ItemDAO Clone(int quantity)
        {
            return InventoryItemRepository.Instance.Create(TemplateId, -1, quantity, Statistics);
        }
    }
}
