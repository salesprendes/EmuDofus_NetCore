using Protocolo.Framework.Database;
using Game.Database.Repository;
using Game.Spell;
using Game.Manager;
namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("spellbookentry")]
    public sealed class SpellBookEntryDAO : DataAccessObject<SpellBookEntryDAO>
    {
        private int _ownerType;
        private long _ownerId;
        private int _spellId;
        private int _level;
        private int _position;


        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int OwnerType
        {
            get => _ownerType;
            set => SetProperty(ref _ownerType, value);
        }
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long OwnerId
        {
            get => _ownerId;
            set => SetProperty(ref _ownerId, value);
        }
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int SpellId
        {
            get => _spellId;
            set => SetProperty(ref _spellId, value);
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
        public int Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private SpellTemplate m_template;
        private SpellLevel m_level;

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
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


