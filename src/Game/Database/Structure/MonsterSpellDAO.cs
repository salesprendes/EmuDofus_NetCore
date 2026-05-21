using Protocolo.Framework.Database;
using Protocolo.Framework.Generic;
using Game.Manager;
using Game.Spell;

namespace Game.Database.Structure
{
    [Table("monstruos_hechizos")]
    public sealed class MonsterSpellDAO : DataAccessObject<MonsterSpellDAO>
    {
        [Key] public int MonsterId { get; set; }
        [Key] public int GradeId   { get; set; }
        [Key] public int SpellId   { get; set; }
        
        public int SpellLevel { get; set; }

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
