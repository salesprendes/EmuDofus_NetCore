using System.Collections.Generic;
using System.Linq;
using System.Text;
using Protocolo.Framework.Database;
using Game.Database.Repository;
using Game.Entity;
using Game.Spell;

namespace Game.Database.Structure
{ 
    /// <summary>
    /// 
    /// </summary>
    public enum CharacterBreedEnum : byte
    {
        BREED_FECA = 1,
        BREED_OSAMODAS = 2,
        BREED_ENUTROF = 3,
        BREED_SRAM = 4,
        BREED_XELOR = 5,
        BREED_ECAFLIP = 6,
        BREED_ENIRIPSA = 7,
        BREED_IOP = 8,
        BREED_CRA = 9,
        BREED_SADIDAS = 10,
        BREED_SACRIEUR = 11,
        BREED_PANDAWA = 12,
    }

    /// <summary>
    /// 
    /// </summary>    
    [Table("characterinstance")]
    public sealed class CharacterDAO : DataAccessObject<CharacterDAO>
    {
        private long _id;
        private int _serverId;
        private string _name;
        private byte _breed;
        private int _color1;
        private int _color2;
        private int _color3;
        private int _skin;
        private int _skinSize;
        private int _vitality;
        private int _wisdom;
        private int _strength;
        private int _intelligence;
        private int _agility;
        private int _chance;
        private int _life;
        private int _energy;
        private int _spellPoint;
        private int _caracPoint;
        private int _mapId;
        private int _cellId;
        private int _restriction;
        private long _experience;
        private long _accountId;
        private bool _dead;
        private int _maxLevel;
        private int _deathCount;
        private int _level;
        private bool _sex;
        private long _kamas;
        private int _savedMapId;
        private int _savedCellId;
        private bool _merchant;
        private int _titleId;
        private string _titleParams;
        private int _emoteCapacity;
        private int _deathType;
        private int _equippedMount;
        private int _alignmentId;
        private int _alignmentLevel;
        private int _alignmentPromotion;
        private int _alignmentHonour;
        private int _alignmentDishonour;
        private bool _alignmentEnabled;
        private string _zaaps;


        /// <summary>
        ///
        /// </summary>
        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int ServerId
        {
            get => _serverId;
            set => SetProperty(ref _serverId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public byte Breed
        {
            get => _breed;
            set => SetProperty(ref _breed, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Color1
        {
            get => _color1;
            set => SetProperty(ref _color1, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Color2
        {
            get => _color2;
            set => SetProperty(ref _color2, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Color3
        {
            get => _color3;
            set => SetProperty(ref _color3, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Skin
        {
            get => _skin;
            set => SetProperty(ref _skin, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int SkinSize
        {
            get => _skinSize;
            set => SetProperty(ref _skinSize, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Vitality
        {
            get => _vitality;
            set => SetProperty(ref _vitality, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Wisdom
        {
            get => _wisdom;
            set => SetProperty(ref _wisdom, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Strength
        {
            get => _strength;
            set => SetProperty(ref _strength, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Intelligence
        {
            get => _intelligence;
            set => SetProperty(ref _intelligence, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Agility
        {
            get => _agility;
            set => SetProperty(ref _agility, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Chance
        {
            get => _chance;
            set => SetProperty(ref _chance, value);
        }

        /// <summary>
        ///
        /// </summary>
        public int Life
        {
            get => _life;
            set => SetProperty(ref _life, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Energy
        {
            get => _energy;
            set => SetProperty(ref _energy, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int SpellPoint
        {
            get => _spellPoint;
            set => SetProperty(ref _spellPoint, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int CaracPoint
        {
            get => _caracPoint;
            set => SetProperty(ref _caracPoint, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int MapId
        {
            get => _mapId;
            set => SetProperty(ref _mapId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int CellId
        {
            get => _cellId;
            set => SetProperty(ref _cellId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Restriction
        {
            get => _restriction;
            set => SetProperty(ref _restriction, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public long Experience
        {
            get => _experience;
            set => SetProperty(ref _experience, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public long AccountId
        {
            get => _accountId;
            set => SetProperty(ref _accountId, value);
        }


        /// <summary>
        /// 
        /// </summary>
        public bool Dead
        {
            get => _dead;
            set => SetProperty(ref _dead, value);
        }


        /// <summary>
        /// 
        /// </summary>
        public int MaxLevel
        {
            get => _maxLevel;
            set => SetProperty(ref _maxLevel, value);
        }


        /// <summary>
        /// 
        /// </summary>
        public int DeathCount
        {
            get => _deathCount;
            set => SetProperty(ref _deathCount, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Sex
        {
            get => _sex;
            set => SetProperty(ref _sex, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public long Kamas
        {
            get => _kamas;
            set => SetProperty(ref _kamas, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int SavedMapId
        {
            get => _savedMapId;
            set => SetProperty(ref _savedMapId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int SavedCellId
        {
            get => _savedCellId;
            set => SetProperty(ref _savedCellId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Merchant
        {
            get => _merchant;
            set => SetProperty(ref _merchant, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int TitleId
        {
            get => _titleId;
            set => SetProperty(ref _titleId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string TitleParams
        {
            get => _titleParams;
            set => SetProperty(ref _titleParams, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int EmoteCapacity
        {
            get => _emoteCapacity;
            set => SetProperty(ref _emoteCapacity, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int DeathType
        {
            get => _deathType;
            set => SetProperty(ref _deathType, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int EquippedMount
        {
            get => _equippedMount;
            set => SetProperty(ref _equippedMount, value);
        }

        public int AlignmentId
        {
            get => _alignmentId;
            set => SetProperty(ref _alignmentId, value);
        }

        public int AlignmentLevel
        {
            get => _alignmentLevel;
            set => SetProperty(ref _alignmentLevel, value);
        }

        public int AlignmentPromotion
        {
            get => _alignmentPromotion;
            set => SetProperty(ref _alignmentPromotion, value);
        }

        public int AlignmentHonour
        {
            get => _alignmentHonour;
            set => SetProperty(ref _alignmentHonour, value);
        }

        public int AlignmentDishonour
        {
            get => _alignmentDishonour;
            set => SetProperty(ref _alignmentDishonour, value);
        }

        public bool AlignmentEnabled
        {
            get => _alignmentEnabled;
            set => SetProperty(ref _alignmentEnabled, value);
        }

        public string Zaaps
        {
            get => _zaaps;
            set => SetProperty(ref _zaaps, value);
        }

        #region Unmapped

        private CharacterGuildDAO m_guild;
        private List<CharacterQuestDAO> m_quests = new List<CharacterQuestDAO>();

        [Write(false)]
        public List<CharacterQuestDAO> Quests => m_quests;

        public void AddQuest(CharacterQuestDAO quest)
        {
            m_quests.Add(quest);
        }

        [Write(false)]
        public string HexColor1
        {
            get
            {
                if (Color1 == -1)
                    return "-1";
                return Color1.ToString("x");
            }
        }

        [Write(false)]
        public string HexColor2
        {
            get
            {
                if (Color2 == -1)
                    return "-1";
                return Color2.ToString("x");
            }
        }

        [Write(false)]
        public string HexColor3
        {
            get
            {
                if (Color3 == -1)
                    return "-1";
                return Color3.ToString("x");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public CharacterGuildDAO Guild
        {
            get
            {
                if (m_guild == null)
                    m_guild = CharacterGuildRepository.Instance.GetById(Id) ?? new CharacterGuildDAO { Id = Id, GuildId = -1 };
                return m_guild;
            }
        }

        public List<int> GetWaypoints()
        {
            if (string.IsNullOrWhiteSpace(Zaaps))
                return new List<int>();

            return Zaaps
                .Split(',')
                .Select(value => int.TryParse(value, out var mapId) ? mapId : -1)
                .Where(mapId => mapId > 0)
                .Distinct()
                .ToList();
        }

        public void SetWaypoints(IEnumerable<int> waypoints)
        {
            Zaaps = string.Join(",", waypoints.Distinct());
        }
           
        private static int GetLivingEffectValue(ItemDAO item, EffectEnum effect, int defaultValue = 0)
        {
            if (item == null || !item.Statistics.HasEffect(effect))
                return defaultValue;

            var value = item.Statistics.GetEffect(effect).Value3;
            return value == 0 ? defaultValue : value;
        }

        private static void AppendLivingAccessory(StringBuilder message, ItemDAO item)
        {
            var livingTemplateId = GetLivingEffectValue(item, EffectEnum.LivingGfxId);

            if (livingTemplateId > 0)
            {
                message.Append(livingTemplateId.ToString("x")).Append('~').Append(item.Template.Type).Append('~').Append(GetLivingEffectValue(item, EffectEnum.LivingSkin, 1));
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
            var items = new List<ItemDAO>(InventoryItemRepository.Instance.GetByOwner((int)EntityTypeEnum.TYPE_CHARACTER, Id));
            var weapon = items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_WEAPON);
            var hat = items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_HAT);
            var cape = items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_CAPE);
            var pet = items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_PET);
            var shield = items.Find(entry => entry.Slot == ItemSlotEnum.SLOT_SHIELD);

            if (weapon != null)
                message.Append(weapon.TemplateId.ToString("x"));
            message.Append(',');

            if (hat != null)
                AppendLivingAccessory(message, hat);
            message.Append(',');

            if (cape != null)
                AppendLivingAccessory(message, cape);
            message.Append(',');

            if (pet != null)
                message.Append(pet.TemplateId.ToString("x"));
            message.Append(',');

            if (shield != null)
                message.Append(shield.TemplateId.ToString("x"));
            message.Append(',');
        }
        #endregion
    }
}
