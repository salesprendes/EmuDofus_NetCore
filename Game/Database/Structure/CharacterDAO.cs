using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Protocolo.Framework.Database;
using Game.Database.Repository;
using Game.Entity;
using Game.Spell;

namespace Game.Database.Structure
{
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
        private string _jobs;
        private DateTime _disconnectedAt;


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

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public byte Breed
        {
            get => _breed;
            set => SetProperty(ref _breed, value);
        }

        public int Color1
        {
            get => _color1;
            set => SetProperty(ref _color1, value);
        }

        public int Color2
        {
            get => _color2;
            set => SetProperty(ref _color2, value);
        }

        public int Color3
        {
            get => _color3;
            set => SetProperty(ref _color3, value);
        }

        public int Skin
        {
            get => _skin;
            set => SetProperty(ref _skin, value);
        }

        public int SkinSize
        {
            get => _skinSize;
            set => SetProperty(ref _skinSize, value);
        }

        public int Vitality
        {
            get => _vitality;
            set => SetProperty(ref _vitality, value);
        }

        public int Wisdom
        {
            get => _wisdom;
            set => SetProperty(ref _wisdom, value);
        }

        public int Strength
        {
            get => _strength;
            set => SetProperty(ref _strength, value);
        }
        public int Intelligence
        {
            get => _intelligence;
            set => SetProperty(ref _intelligence, value);
        }

        public int Agility
        {
            get => _agility;
            set => SetProperty(ref _agility, value);
        }

        public int Chance
        {
            get => _chance;
            set => SetProperty(ref _chance, value);
        }

        public int Life
        {
            get => _life;
            set => SetProperty(ref _life, value);
        }

        public int Energy
        {
            get => _energy;
            set => SetProperty(ref _energy, value);
        }

        public int SpellPoint
        {
            get => _spellPoint;
            set => SetProperty(ref _spellPoint, value);
        }

        public int CaracPoint
        {
            get => _caracPoint;
            set => SetProperty(ref _caracPoint, value);
        }

        public int MapId
        {
            get => _mapId;
            set => SetProperty(ref _mapId, value);
        }

        public int CellId
        {
            get => _cellId;
            set => SetProperty(ref _cellId, value);
        }

        public int Restriction
        {
            get => _restriction;
            set => SetProperty(ref _restriction, value);
        }

        public long Experience
        {
            get => _experience;
            set => SetProperty(ref _experience, value);
        }

        public long AccountId
        {
            get => _accountId;
            set => SetProperty(ref _accountId, value);
        }


        public bool Dead
        {
            get => _dead;
            set => SetProperty(ref _dead, value);
        }


        public int MaxLevel
        {
            get => _maxLevel;
            set => SetProperty(ref _maxLevel, value);
        }


        public int DeathCount
        {
            get => _deathCount;
            set => SetProperty(ref _deathCount, value);
        }

        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        public bool Sex
        {
            get => _sex;
            set => SetProperty(ref _sex, value);
        }

        public long Kamas
        {
            get => _kamas;
            set => SetProperty(ref _kamas, value);
        }

        public int SavedMapId
        {
            get => _savedMapId;
            set => SetProperty(ref _savedMapId, value);
        }

        public int SavedCellId
        {
            get => _savedCellId;
            set => SetProperty(ref _savedCellId, value);
        }

        public bool Merchant
        {
            get => _merchant;
            set => SetProperty(ref _merchant, value);
        }

        public int TitleId
        {
            get => _titleId;
            set => SetProperty(ref _titleId, value);
        }

        public string TitleParams
        {
            get => _titleParams;
            set => SetProperty(ref _titleParams, value);
        }

        public int EmoteCapacity
        {
            get => _emoteCapacity;
            set => SetProperty(ref _emoteCapacity, value);
        }

        public int DeathType
        {
            get => _deathType;
            set => SetProperty(ref _deathType, value);
        }

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

        public string Jobs
        {
            get => _jobs;
            set => SetProperty(ref _jobs, value);
        }

        public DateTime DisconnectedAt
        {
            get => _disconnectedAt;
            set => SetProperty(ref _disconnectedAt, value);
        }

        #region Unmapped

        private CharacterGuildDAO m_guild;
        private List<CharacterQuestDAO> m_quests = new List<CharacterQuestDAO>();

        [Write(false)] public List<CharacterQuestDAO> Quests => m_quests;

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

            return Zaaps.Split(',').Select(value => int.TryParse(value, out var mapId) ? mapId : -1).Where(mapId => mapId > 0).Distinct().ToList();
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
