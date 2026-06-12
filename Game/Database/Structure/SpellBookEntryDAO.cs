using Protocolo.Framework.Database;
using Game.Database.Repository;
using Game.Spell;
using Game.Manager;
namespace Game.Database.Structure
{
    [Table("spellbookentry")]
    public sealed class SpellBookEntryDAO : DataAccessObject<SpellBookEntryDAO>
    {
        private int _ownerType;
        private long _ownerId;
        private int _spellId;
        private int _level;
        private int _position;


        [Key]
        public int OwnerType
        {
            get => _ownerType;
            set => SetProperty(ref _ownerType, value);
        }
        [Key]
        public long OwnerId
        {
            get => _ownerId;
            set => SetProperty(ref _ownerId, value);
        }
        [Key]
        public int SpellId
        {
            get => _spellId;
            set => SetProperty(ref _spellId, value);
        }
        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }
        public int Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private SpellTemplate m_template;
        private SpellLevel m_level;

        [Write(false)]
        public SpellTemplate Template
        {
            get
            {
                if (m_template == null)
                    m_template = SpellManager.Instance.GetTemplate(SpellId);
                return m_template;
            }
        }

        [Write(false)]
        public SpellLevel SpellLevel
        {
            get
            {
                if (m_level == null || Level != m_level.Level)
                    m_level = Template.GetLevel(Level);
                return m_level;
            }
        }
    }
}


