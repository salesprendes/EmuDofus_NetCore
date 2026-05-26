using Protocolo.Framework.Database;
using Protocolo.Framework.Generic;
using Game.Manager;
using Game.Spell;

namespace Game.Database.Structure
{
    [Table("monstruos_hechizos")]
    public sealed class MonsterSpellDAO : DataAccessObject<MonsterSpellDAO>
    {
        private int _monsterId;
        private int _gradeId;
        private int _spellId;
        private int _spellLevel;


        [Key] public int MonsterId
        {
            get => _monsterId;
            set => SetProperty(ref _monsterId, value);
        }
        [Key] public int GradeId
        {
            get => _gradeId;
            set => SetProperty(ref _gradeId, value);
        }
        [Key] public int SpellId
        {
            get => _spellId;
            set => SetProperty(ref _spellId, value);
        }
        
        public int SpellLevel
        {
            get => _spellLevel;
            set => SetProperty(ref _spellLevel, value);
        }

        private SpellTemplate m_template;
        private SpellLevel m_combatLevel;

        [Write(false)]
        public SpellTemplate Template
        {
            get
            {
                if (m_template == null)
                {
                    m_template = SpellManager.Instance.GetTemplate(SpellId);
                    if (m_template == null)
                        Logger.Info($"[MonsterSpell] Hechizo SpellId={SpellId} no existe (MonsterId={MonsterId}, GradeId={GradeId}).");
                }
                return m_template;
            }
        }

        /// <summary>The resolved SpellLevel object used by the combat engine.</summary>
        [Write(false)]
        public SpellLevel CombatLevel
        {
            get
            {
                if (m_combatLevel == null || SpellLevel != m_combatLevel.Level)
                {
                    m_combatLevel = Template?.GetLevel(SpellLevel);
                    if (m_combatLevel == null && Template != null)
                        Logger.Info($"[MonsterSpell] Nivel {SpellLevel} del hechizo SpellId={SpellId} no existe (MonsterId={MonsterId}, GradeId={GradeId}).");
                }
                return m_combatLevel;
            }
        }
    }
}
