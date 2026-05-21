using Game.Database.Repository;
using Game.Database.Structure;
using Game.Stats;
using Game.Network;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entity.Inventory
{
    /// <summary>
    /// 
    /// </summary>
    public class EntityInventory : PersistentInventory
    {
        /// <summary>
        /// 
        /// </summary>
        public override long Kamas
        {
            get
            {
                return Entity.Kamas;
            }
            set
            {
                Entity.Kamas = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public AbstractEntity Entity
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<int, int> m_equippedSets;

        /// <summary>
        /// 
        /// </summary>
        private StringBuilder m_entityLookCache;

        /// <summary>
        /// 
        /// </summary>
        private bool m_entityLookRefresh;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        public EntityInventory(AbstractEntity entity, int type, long id)
            : base(type, id)
        {
            m_equippedSets = new Dictionary<int, int>();

            Entity = entity;
            AddHandler(Entity.Dispatch);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            var now = DateTime.Now;
            foreach (var item in Items)
            {
                var changed = item.RefreshTemporaryExchangeLock(now);
                if (RefreshLivingMoodFromMeal(item, now))
                    changed = true;

                if (changed)
                    item.SaveStats();
            }

            foreach (var item in Items)
            {
                if (item.IsEquiped)
                {
                    AddSet(item);
                    if (item.IsBoostEquiped)
                        Entity.Statistics.Merge(StatsType.TYPE_BOOST, item.Statistics);
                    else
                        Entity.Statistics.Merge(StatsType.TYPE_ITEM, item.Statistics);

                    if (item.Slot == ItemSlotEnum.SLOT_WEAPON)
                    {
                        if (Entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                        {
                            var character = (CharacterEntity)Entity;
                            character.CharacterJobs.ToolEquipped(item.TemplateId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public override IEnumerable<ItemDAO> RemoveItems()
        {
            CachedBuffer = true;
            foreach (var item in Items.ToArray())
            {
                if (item.IsEquiped)
                    Entity.Statistics.UnMerge(item.IsBoostEquiped ? StatsType.TYPE_BOOST : StatsType.TYPE_ITEM,
                        item.Statistics);
                item.SlotId = (int)ItemSlotEnum.SLOT_INVENTORY;
                yield return base.RemoveItem(item.Id, item.Quantity);
            }
            CachedBuffer = false;
            m_entityLookRefresh = true;
        }    
 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public override ItemDAO RemoveItem(long itemId, int quantity = 1)
        {
            var item = Items.Find(entry => entry.Id == itemId);
            if (item == null)
                return null;

            if (item.IsEquiped)
                MoveItem(item, ItemSlotEnum.SLOT_INVENTORY);            

            return base.RemoveItem(itemId, quantity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="slot"></param>
        /// <param name="quantity"></param>
        public void MoveItem(ItemDAO item, ItemSlotEnum slot, int quantity = 1)
        {
            if (slot == item.Slot)
                return;

            if (quantity > item.Quantity || quantity < 1)
                quantity = item.Quantity;

            if (item.IsEquiped && !ItemDAO.IsEquipedSlot(slot))
            {
                if (item.IsBoostEquiped)
                    Entity.Statistics.UnMerge(StatsType.TYPE_BOOST, item.Statistics);
                else
                    Entity.Statistics.UnMerge(StatsType.TYPE_ITEM, item.Statistics);

                if (item.Slot == ItemSlotEnum.SLOT_WEAPON)
                {
                    Entity.Dispatch(WorldMessage.JOB_TOOL_EQUIPPED());
                }

                item.SlotId = (int)slot;
                m_entityLookRefresh = true;
                bool merged = AddItem(MoveQuantity(item, 1));

                RemoveSet(item);

                // send new stats
                if (Entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                {
                    Entity.MovementHandler.Dispatch(WorldMessage.ENTITY_OBJECT_ACTUALIZE(Entity));

                    CachedBuffer = true;
                    if (!merged)
                        Dispatch(WorldMessage.OBJECT_MOVE_SUCCESS(item.Id, item.SlotId));
                    Dispatch(WorldMessage.ACCOUNT_STATS((CharacterEntity)Entity));
                    if (item.Template.SetId != 0)                    
                        Dispatch(WorldMessage.ITEM_SET(item.Template.Set, Items.Where(entry => entry.Template.SetId == item.Template.SetId && entry.IsEquiped)));                    
                    CachedBuffer = false;
                }
                return;
            }
            else if (!item.IsEquiped && ItemDAO.IsEquipedSlot(slot))
            {
                if ((ItemTypeEnum)item.Template.Type == ItemTypeEnum.TYPE_OBJET_VIVANT)
                {
                    AssociateLivingItem(item, slot);
                    return;
                }

                if (!ItemTemplateDAO.CanPlaceInSlot((ItemTypeEnum)item.Template.Type, slot))
                {
                    base.Dispatch(WorldMessage.OBJECT_MOVE_ERROR());
                    return;
                }

                // level required
                if (Entity.Level < item.Template.Level)
                {
                    base.Dispatch(WorldMessage.OBJECT_MOVE_ERROR_REQUIRED_LEVEL());
                    return;
                }

                // Already equiped template                    
                if (HasTemplateEquiped(item.TemplateId))
                {
                    base.Dispatch(WorldMessage.OBJECT_MOVE_ERROR_ALREADY_EQUIPED());
                    return;
                }

                if (Entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                {
                    if (!item.SatisfyConditions((CharacterEntity)Entity))
                    {
                        base.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_CONDITIONS_UNSATISFIED));
                        return;
                    }
                }

                var equipedItem = Items.Find(entry => entry.SlotId == (int)slot && entry.Id != item.Id);

                // already equiped in slot ? remove it
                if (equipedItem != null)
                {
                    MoveItem(equipedItem, ItemSlotEnum.SLOT_INVENTORY);
                }
                
                m_entityLookRefresh = true;
                var newItem = MoveQuantity(item, 1);
                newItem.SlotId = (int)slot;
                AddItem(newItem, false);

                AddSet(newItem);

                if (item.IsBoostEquiped)
                    Entity.Statistics.Merge(StatsType.TYPE_BOOST, item.Statistics);
                else
                    Entity.Statistics.Merge(StatsType.TYPE_ITEM, item.Statistics);
               
                // send new stats
                if (Entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                {
                    Entity.MovementHandler.Dispatch(WorldMessage.ENTITY_OBJECT_ACTUALIZE(Entity));

                    base.CachedBuffer = true;
                    Entity.MovementHandler.Dispatch(WorldMessage.ENTITY_OBJECT_ACTUALIZE(Entity));
                    base.Dispatch(WorldMessage.ACCOUNT_STATS((CharacterEntity)Entity));
                    if(item.Template.SetId != 0)                    
                        base.Dispatch(WorldMessage.ITEM_SET(item.Template.Set, Items.Where(entry => entry.Template.SetId == item.Template.SetId && entry.IsEquiped)));
                    if (newItem.Slot == ItemSlotEnum.SLOT_WEAPON)
                    {
                        var character = (CharacterEntity)Entity;
                        character.CharacterJobs.ToolEquipped(item.TemplateId);
                    }
                    base.CachedBuffer = false;
                }
            }
            else
            {
                var newItem = MoveQuantity(item, quantity);
                newItem.SlotId = (int)slot;
                if(!AddItem(newItem, false))
                   base.Dispatch(WorldMessage.OBJECT_MOVE_SUCCESS(item.Id, item.SlotId));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendSets()
        {
            if (Entity.Type == EntityTypeEnum.TYPE_CHARACTER)
            {
                base.CachedBuffer = true;
                foreach (var set in m_equippedSets)
                    if (set.Value > 0)
                        base.Dispatch(WorldMessage.ITEM_SET(ItemSetRepository.Instance.GetSetById(set.Key), Items.Where(entry => entry.Template.SetId == set.Key && entry.IsEquiped)));
                base.CachedBuffer = false;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void AddSet(ItemDAO item)
        {
            if (item.Template.SetId == 0 || item.Template.Set == null)
                return;

            var set = item.Template.Set;
            if (!m_equippedSets.ContainsKey(set.Id))
                m_equippedSets.Add(set.Id, 0);
            var count = ++m_equippedSets[set.Id];
            
            if (count > 0)
            {
                Entity.Statistics.UnMerge(Stats.StatsType.TYPE_ITEM, set.GetStats(count - 1));
                Entity.Statistics.Merge(Stats.StatsType.TYPE_ITEM, set.GetStats(count));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveSet(ItemDAO item)
        {
            if (item.Template.SetId == 0 || item.Template.Set == null)
                return;

            var set = item.Template.Set;
            var count = --m_equippedSets[set.Id];

            if (count > 0)
            {
                Entity.Statistics.Merge(Stats.StatsType.TYPE_ITEM, set.GetStats(count));
                Entity.Statistics.UnMerge(Stats.StatsType.TYPE_ITEM, set.GetStats(count + 1));
            }
        }

        private const int LIVING_MOOD_LEAN = 0;
        private const int LIVING_MOOD_SATISFIED = 1;
        private const int LIVING_MOOD_FAT = 2;
        private static readonly TimeSpan LIVING_FEED_INTERVAL = TimeSpan.FromHours(12);

        private static bool IsLivingItem(ItemDAO item)
        {
            return item != null && (ItemTypeEnum)item.Template.Type == ItemTypeEnum.TYPE_OBJET_VIVANT;
        }

        private static bool IsLivingAssociated(ItemDAO item)
        {
            return item != null && !IsLivingItem(item) && item.Statistics.HasEffect(EffectEnum.LivingGfxId);
        }

        private static int GetLivingEffectValue(ItemDAO item, EffectEnum effect, int defaultValue = 0, bool zeroIsDefault = true)
        {
            if (item == null || !item.Statistics.HasEffect(effect))
                return defaultValue;

            var value = item.Statistics.GetEffect(effect).Value3;
            return zeroIsDefault && value == 0 ? defaultValue : value;
        }

        private static void SetLivingEffectValue(ItemDAO item, EffectEnum effect, int value)
        {
            var itemEffect = item.Statistics.GetEffect(effect);
            itemEffect.Value1 = 0;
            itemEffect.Value2 = 0;
            itemEffect.Value3 = value;
            itemEffect.Args = "0";
        }

        private static void RemoveLivingEffects(ItemDAO item)
        {
            item.Statistics.RemoveEffect(EffectEnum.LivingGfxId);
            item.Statistics.RemoveEffect(EffectEnum.LivingMood);
            item.Statistics.RemoveEffect(EffectEnum.LivingSkin);
            item.Statistics.RemoveEffect(EffectEnum.LivingType);
            item.Statistics.RemoveEffect(EffectEnum.LivingXp);
            item.Statistics.RemoveEffect(EffectEnum.Received);
            item.Statistics.RemoveEffect(EffectEnum.LastEat);
            item.Statistics.RemoveEffect(EffectEnum.CanBeExchange);
        }

        private static int GetLivingMaxSkinValue(int xp)
        {
            return ExperienceTemplateRepository.Instance.GetLivingLevel(NormalizeLivingExperience(xp));
        }

        private static int NormalizeLivingExperience(int xp)
        {
            var maxExperience = ExperienceTemplateRepository.Instance.GetLivingMaxExperience();
            if (maxExperience <= 0)
                return Math.Max(0, xp);

            return Math.Max(0, Math.Min(xp, maxExperience));
        }

        private static void CopyLivingEffect(ItemDAO source, ItemDAO target, EffectEnum effect)
        {
            if (!source.Statistics.HasEffect(effect))
                return;

            var sourceEffect = source.Statistics.GetEffect(effect);
            var targetEffect = target.Statistics.GetEffect(effect);
            targetEffect.Value1 = sourceEffect.Value1;
            targetEffect.Value2 = sourceEffect.Value2;
            targetEffect.Value3 = sourceEffect.Value3;
            targetEffect.Args = sourceEffect.Args ?? "0";
        }

        private static void CopyLivingEffect(ItemDAO source, GenericStats targetStats, EffectEnum effect)
        {
            if (!source.Statistics.HasEffect(effect))
                return;

            var sourceEffect = source.Statistics.GetEffect(effect);
            targetStats.AddEffect(effect, sourceEffect.Value1, sourceEffect.Value2, sourceEffect.Value3, sourceEffect.Args ?? "0");
        }

        private static bool EnsureLivingReceptionStats(ItemDAO item, DateTime now)
        {
            return ItemDAO.EnsureLivingReceptionStats(item.Statistics, now);
        }

        private static bool CanLivingItemEat(ItemDAO item, DateTime now)
        {
            var lastMeal = ItemDAO.GetDateEffect(item.Statistics, EffectEnum.LastEat);
            return lastMeal == null || lastMeal.Value.Add(LIVING_FEED_INTERVAL) <= now;
        }

        private static bool SetLivingMood(ItemDAO item, int mood)
        {
            var currentMood = GetLivingEffectValue(item, EffectEnum.LivingMood, LIVING_MOOD_SATISFIED, false);
            if (currentMood == mood)
                return false;

            SetLivingEffectValue(item, EffectEnum.LivingMood, mood);
            return true;
        }

        private static bool RefreshLivingMoodFromMeal(ItemDAO item, DateTime now)
        {
            if ((!IsLivingItem(item) && !IsLivingAssociated(item)) || !item.Statistics.HasEffect(EffectEnum.LivingMood))
                return false;

            var lastMeal = ItemDAO.GetDateEffect(item.Statistics, EffectEnum.LastEat);
            if (lastMeal == null)
                return SetLivingMood(item, LIVING_MOOD_LEAN);

            var currentMood = GetLivingEffectValue(item, EffectEnum.LivingMood, LIVING_MOOD_LEAN, false);
            if (lastMeal.Value.Add(LIVING_FEED_INTERVAL) <= now)
            {
                if (currentMood == LIVING_MOOD_FAT)
                    return SetLivingMood(item, LIVING_MOOD_LEAN);
                return false;
            }

            if (currentMood == LIVING_MOOD_LEAN)
                return SetLivingMood(item, LIVING_MOOD_SATISFIED);
            return false;
        }

        private static GenericStats CreateDetachedLivingStats(ItemDAO associatedItem)
        {
            var stats = new GenericStats();
            stats.AddEffect(EffectEnum.LivingMood, 0, 0, GetLivingEffectValue(associatedItem, EffectEnum.LivingMood, LIVING_MOOD_SATISFIED, false));
            stats.AddEffect(EffectEnum.LivingSkin, 0, 0, GetLivingEffectValue(associatedItem, EffectEnum.LivingSkin, 1));
            stats.AddEffect(EffectEnum.LivingType, 0, 0, GetLivingEffectValue(associatedItem, EffectEnum.LivingType, associatedItem.Template.Type));
            stats.AddEffect(EffectEnum.LivingXp, 0, 0, GetLivingEffectValue(associatedItem, EffectEnum.LivingXp));
            CopyLivingEffect(associatedItem, stats, EffectEnum.Received);
            CopyLivingEffect(associatedItem, stats, EffectEnum.LastEat);
            CopyLivingEffect(associatedItem, stats, EffectEnum.CanBeExchange);
            return stats;
        }

        private void RefreshLivingItem(ItemDAO item, bool refreshLook = true)
        {
            CachedBuffer = true;
            Dispatch(WorldMessage.OBJECT_UPDATE(item));
            Dispatch(WorldMessage.LIVING_ITEM_UPDATE(item));
            CachedBuffer = false;

            if (refreshLook)
                RefreshEntityLook();
        }

        private void RefreshEntityLook()
        {
            m_entityLookRefresh = true;

            if (Entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                Entity.MovementHandler.Dispatch(WorldMessage.ENTITY_OBJECT_ACTUALIZE(Entity));
        }

        private void DispatchLivingAssociationError(InformationEnum information)
        {
            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, information));
            Dispatch(WorldMessage.OBJECT_MOVE_ERROR());
        }

        private void AssociateLivingItem(ItemDAO livingItem, ItemSlotEnum slot)
        {
            var targetItem = Items.Find(entry => entry.Slot == slot && entry.Id != livingItem.Id);
            var livingType = GetLivingEffectValue(livingItem, EffectEnum.LivingType);
            var now = DateTime.Now;

            if (targetItem == null)
            {
                DispatchLivingAssociationError(InformationEnum.INFO_LIVING_ITEM_CANT_EQUIP);
                return;
            }

            if (IsLivingItem(targetItem) || IsLivingAssociated(targetItem) || livingType != targetItem.Template.Type)
            {
                DispatchLivingAssociationError(InformationEnum.INFO_LIVING_ITEM_CANT_ASSOCIATE_INANIMATE);
                return;
            }

            var livingDatesChanged = EnsureLivingReceptionStats(livingItem, now);
            if (livingItem.RefreshTemporaryExchangeLock(now))
                livingDatesChanged = true;
            var livingXp = NormalizeLivingExperience(GetLivingEffectValue(livingItem, EffectEnum.LivingXp));
            var livingSkin = GetLivingEffectValue(livingItem, EffectEnum.LivingSkin, 1);
            var maxSkin = GetLivingMaxSkinValue(livingXp);

            if (livingSkin < 1)
                livingSkin = 1;
            if (livingSkin > maxSkin)
                livingSkin = maxSkin;

            SetLivingEffectValue(targetItem, EffectEnum.LivingGfxId, livingItem.TemplateId);
            SetLivingEffectValue(targetItem, EffectEnum.LivingMood, GetLivingEffectValue(livingItem, EffectEnum.LivingMood, LIVING_MOOD_SATISFIED, false));
            SetLivingEffectValue(targetItem, EffectEnum.LivingSkin, livingSkin);
            SetLivingEffectValue(targetItem, EffectEnum.LivingType, livingType);
            SetLivingEffectValue(targetItem, EffectEnum.LivingXp, livingXp);
            CopyLivingEffect(livingItem, targetItem, EffectEnum.Received);
            CopyLivingEffect(livingItem, targetItem, EffectEnum.LastEat);
            CopyLivingEffect(livingItem, targetItem, EffectEnum.CanBeExchange);
            targetItem.SaveStats();

            if (livingDatesChanged)
            {
                livingItem.SaveStats();
                if (livingItem.Quantity > 1)
                    Dispatch(WorldMessage.OBJECT_UPDATE(livingItem));
            }

            RemoveItem(livingItem.Id, 1);
            RefreshLivingItem(targetItem);
        }

        /// <summary>
        /// Feeds an associated living item with an item of the same real type.
        /// Client packet: Of{itemId}|{slot}|{foodItemId}
        /// </summary>
        public void FeedLivingItem(long associatedItemId, long foodItemId)
        {
            var associatedItem = Items.Find(x => x.Id == associatedItemId);
            var isStandalone = IsLivingItem(associatedItem);
            if (!IsLivingAssociated(associatedItem) && !isStandalone)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var foodItem = Items.Find(x => x.Id == foodItemId);
            // For standalone vivants, LivingType must be read from the effect directly (template type is TYPE_OBJET_VIVANT = 113).
            // For associated items, fall back to host item's template type if the effect is missing.
            var livingType = isStandalone
                ? GetLivingEffectValue(associatedItem, EffectEnum.LivingType, 0, false)
                : GetLivingEffectValue(associatedItem, EffectEnum.LivingType, associatedItem.Template.Type);

            if (livingType == 0 || foodItem == null || foodItem.Id == associatedItem.Id || foodItem.IsEquiped || foodItem.Template.Type != livingType || IsLivingItem(foodItem) || IsLivingAssociated(foodItem))
            {
                Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_LIVING_ITEM_WONT_EAT));
                return;
            }

            var now = DateTime.Now;
            var metadataChanged = EnsureLivingReceptionStats(associatedItem, now);
            if (associatedItem.RefreshTemporaryExchangeLock(now))
                metadataChanged = true;

            var currentMood = GetLivingEffectValue(associatedItem, EffectEnum.LivingMood, LIVING_MOOD_LEAN, false);

            // FAT: cannot eat at all until the 12h cycle resets back to LEAN
            // LEAN: can only eat if 12h have passed since the last cycle (CanLivingItemEat)
            // SATISFIED: can eat again immediately (no cooldown) to reach FAT
            if (currentMood == LIVING_MOOD_FAT || (currentMood == LIVING_MOOD_LEAN && !CanLivingItemEat(associatedItem, now)))
            {
                if (metadataChanged)
                {
                    associatedItem.SaveStats();
                    RefreshLivingItem(associatedItem);
                }
                Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_LIVING_ITEM_WONT_EAT));
                return;
            }

            // Advance mood: LEAN → SATISFIED (1st meal), SATISFIED → FAT (2nd meal, immediate)
            int newMood = currentMood == LIVING_MOOD_LEAN ? LIVING_MOOD_SATISFIED : LIVING_MOOD_FAT;
            SetLivingMood(associatedItem, newMood);
            ItemDAO.SetDateEffect(associatedItem.Statistics, EffectEnum.LastEat, now);

            var currentXp = GetLivingEffectValue(associatedItem, EffectEnum.LivingXp);
            var newXp = NormalizeLivingExperience(currentXp + Math.Max(1, foodItem.Template.Level / 3));
            SetLivingEffectValue(associatedItem, EffectEnum.LivingXp, newXp);

            var maxSkin = GetLivingMaxSkinValue(newXp);
            var currentSkin = GetLivingEffectValue(associatedItem, EffectEnum.LivingSkin, 1);
            if (currentSkin > maxSkin)
                SetLivingEffectValue(associatedItem, EffectEnum.LivingSkin, maxSkin);

            associatedItem.SaveStats();

            RemoveItem(foodItemId, 1);
            RefreshLivingItem(associatedItem);
        }

        /// <summary>
        /// Changes the visual skin of an associated living item.
        /// Client packet: Os{itemId}|{slot}|{skinId}
        /// </summary>
        public void SetLivingItemSkin(long associatedItemId, int skinId)
        {
            var associatedItem = Items.Find(x => x.Id == associatedItemId);
            if (!IsLivingAssociated(associatedItem))
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var maxSkin = GetLivingMaxSkinValue(GetLivingEffectValue(associatedItem, EffectEnum.LivingXp));

            if (skinId < 1)
                skinId = 1;

            if (skinId > maxSkin)
                skinId = maxSkin;

            SetLivingEffectValue(associatedItem, EffectEnum.LivingSkin, skinId);
            associatedItem.SaveStats();

            RefreshLivingItem(associatedItem);
        }

        /// <summary>
        /// Dissociates a living item from its host item and recreates the detached living object.
        /// Client packet: Ox{itemId}|{slot}
        /// </summary>
        public void DissociateLivingItem(long associatedItemId)
        {
            var associatedItem = Items.Find(x => x.Id == associatedItemId);
            if (!IsLivingAssociated(associatedItem))
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var livingTemplateId = GetLivingEffectValue(associatedItem, EffectEnum.LivingGfxId);
            var livingTemplate = ItemTemplateRepository.Instance.GetById(livingTemplateId);
            if (livingTemplate == null || (ItemTypeEnum)livingTemplate.Type != ItemTypeEnum.TYPE_OBJET_VIVANT)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var now = DateTime.Now;
            EnsureLivingReceptionStats(associatedItem, now);
            associatedItem.RefreshTemporaryExchangeLock(now);

            var livingStats = CreateDetachedLivingStats(associatedItem);
            var livingItem = InventoryItemRepository.Instance.Create(livingTemplateId, OwnerId, 1, livingStats);

            RemoveLivingEffects(associatedItem);
            associatedItem.SaveStats();

            CachedBuffer = true;
            AddItem(livingItem, false);
            Dispatch(WorldMessage.OBJECT_UPDATE(associatedItem));
            Dispatch(WorldMessage.LIVING_ITEM_UPDATE(associatedItem));
            CachedBuffer = false;

            RefreshEntityLook();
        }

        private static void AppendLivingAccessory(StringBuilder message, ItemDAO item)
        {
            var livingTemplateId = GetLivingEffectValue(item, EffectEnum.LivingGfxId);
            if (livingTemplateId > 0)
            {
                message
                    .Append(livingTemplateId.ToString("x"))
                    .Append('~')
                    .Append(item.Template.Type)
                    .Append('~')
                    .Append(GetLivingEffectValue(item, EffectEnum.LivingSkin, 1));
                return;
            }

            message.Append(item.TemplateId.ToString("x"));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public void SerializeAs_ActorLookMessage(StringBuilder message)
        {
            if (m_entityLookRefresh || m_entityLookCache == null)
            {
                m_entityLookCache = new StringBuilder();

                var weapon = Items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_WEAPON);
                var hat = Items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_HAT);
                var cape = Items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_CAPE);
                var pet = Items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_PET);
                var shield = Items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_SHIELD);

                if (weapon != null)
                    m_entityLookCache.Append(weapon.TemplateId.ToString("x"));
                m_entityLookCache.Append(',');
                if (hat != null)
                    AppendLivingAccessory(m_entityLookCache, hat);
                m_entityLookCache.Append(',');
                if (cape != null)
                    AppendLivingAccessory(m_entityLookCache, cape);
                m_entityLookCache.Append(',');
                if (pet != null)
                    m_entityLookCache.Append(pet.TemplateId.ToString("x"));
                m_entityLookCache.Append(',');
                if (shield != null)
                    m_entityLookCache.Append(shield.TemplateId.ToString("x"));
                m_entityLookCache.Append(',');

                m_entityLookRefresh = false;
            }
            message.Append(m_entityLookCache.ToString());
        }
    }
}
