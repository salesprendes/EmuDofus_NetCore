using Protocolo.Framework.Database;
using Protocolo.Framework.Generic;
using Game.Database.Structure;
using System.Collections.Generic;
using System.Linq;

namespace Game.Database.Repository
{
    public sealed class MonsterSpellRepository : Repository<MonsterSpellRepository, MonsterSpellDAO>
    {
        private readonly Dictionary<long, List<MonsterSpellDAO>> m_spellsByKey;

        public MonsterSpellRepository()
        {
            m_spellsByKey = new Dictionary<long, List<MonsterSpellDAO>>();
        }

        private static long MakeKey(int monsterId, int gradeId) =>
            ((long)monsterId << 32) | (uint)gradeId;

        public override void OnObjectAdded(MonsterSpellDAO spell)
        {
            var key = MakeKey(spell.MonsterId, spell.GradeId);
            if (!m_spellsByKey.TryGetValue(key, out var list))
            {
                list = new List<MonsterSpellDAO>();
                m_spellsByKey.Add(key, list);
            }
            list.Add(spell);
        }

        public override void OnObjectRemoved(MonsterSpellDAO spell)
        {
            var key = MakeKey(spell.MonsterId, spell.GradeId);
            if (m_spellsByKey.TryGetValue(key, out var list))
                list.Remove(spell);
        }

        public IEnumerable<MonsterSpellDAO> GetByMonsterAndGrade(int monsterId, int gradeId)
        {
            var key = MakeKey(monsterId, gradeId);
            if (m_spellsByKey.TryGetValue(key, out var list))
                return list;
            Logger.Info($"[MonsterSpell] MonsterId={monsterId} GradeId={gradeId} no tiene hechizos en la tabla monstruos_hechizos.");
            return Enumerable.Empty<MonsterSpellDAO>();
        }
    }
}
