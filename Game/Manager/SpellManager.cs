using System.Collections.Generic;
using System.Linq;
using System.Text;
using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Database.Repository;
using Game.Spell;

namespace Game.Manager
{
    public sealed class SpellManager : Singleton<SpellManager>
    {
        private readonly Dictionary<int, SpellTemplate> m_templateById = new Dictionary<int, SpellTemplate>();

        public void Initialize()
        {
            foreach (var dao in SpellRepository.Instance.All)
            {
                var template = new SpellTemplate
                {
                    Id = dao.id,
                    Name = dao.nombre,
                    Description = dao.descripcion,
                    Sprite = dao.sprite,
                    SpriteInfos = dao.spriteInfos,
                    Conditions = dao.condiciones,
                    Targets = ParseTargets(dao.afectados),
                    Levels = new List<SpellLevel>()
                };

                var levelStrings = new[] { dao.nivel1, dao.nivel2, dao.nivel3, dao.nivel4, dao.nivel5, dao.nivel6 };
                for (int i = 0; i < levelStrings.Length; i++)
                {
                    var level = ParseLevel(levelStrings[i], dao.id, i + 1);
                    if (level != null)
                        template.Levels.Add(level);
                }

                m_templateById[template.Id] = template;
            }

            Logger.Info("SpellManager : " + m_templateById.Count + " SpellTemplate loaded.");
        }

        public SpellLevel GetSpellLevel(int spellId, int spellLevel)
        {
            SpellTemplate spell = null;
            SpellLevel level = null;
            if (m_templateById.TryGetValue(spellId, out spell))
                level = spell.GetLevel(spellLevel);
            return level;
        }

        public SpellTemplate GetTemplate(int spellId)
        {
            SpellTemplate spell = null;
            m_templateById.TryGetValue(spellId, out spell);
            return spell;
        }

        public IEnumerable<SpellBookEntryDAO> GetSpells(int ownerType, long ownerId)
        {
            return SpellBookEntryRepository.Instance.GetSpellEntries(ownerType, ownerId);
        }

        // --- Parsers ---

        private static List<int> ParseTargets(string afectados)
        {
            if (string.IsNullOrEmpty(afectados))
                return new List<int>();
            return afectados.Split('|')
                .Select(x => { int.TryParse(x.Trim(), out int v); return v; })
                .ToList();
        }

        private static SpellLevel ParseLevel(string s, int spellId, int levelNum)
        {
            if (string.IsNullOrEmpty(s) || s.Trim() == "-1" || s.Trim() == "[]")
                return null;

            s = s.Trim();
            if (s.Length < 2 || s[0] != '[')
                return null;

            var parts = SplitTopLevel(s.Substring(1, s.Length - 2));
            if (parts.Count < 20)
                return null;

            return new SpellLevel
            {
                SpellId = spellId,
                Level = levelNum,
                Effects = ParseEffects(parts[0], spellId, levelNum),
                CriticalEffects = ParseEffects(parts[1], spellId, levelNum),
                APCost = ParseInt(parts[2]),
                MinPO = ParseInt(parts[3]),
                MaxPO = ParseInt(parts[4]),
                CSRate = ParseInt(parts[5]),
                ECSRate = ParseInt(parts[6]),
                InLine = ParseBool(parts[7]),
                LOS = ParseBool(parts[8]),
                EmptyCell = ParseBool(parts[9]),
                AllowPOBoost = ParseBool(parts[10]),
                MaxLaunchPerGame = ParseInt(parts[11]),
                MaxLaunchPerTurn = ParseInt(parts[12]),
                MaxLaunchPerTarget = ParseInt(parts[13]),
                Cooldown = ParseInt(parts[14]),
                RangeType = parts[15].Trim(),
                Conditions = ParseIntList(parts[16]),
                TargetZones = ParseIntList(parts[17]),
                RequiredLevel = ParseInt(parts[18]),
                IsECSEndTurn = ParseBool(parts[19]) ? 1 : 0,
            };
        }

        private static List<SpellEffect> ParseEffects(string s, int spellId, int levelNum)
        {
            var effects = new List<SpellEffect>();
            s = s.Trim();
            if (string.IsNullOrEmpty(s) || s == "[]")
                return effects;

            // Strip outer array brackets: [[e1],[e2]] -> [e1],[e2]
            s = s.Substring(1, s.Length - 2).Trim();
            if (string.IsNullOrEmpty(s))
                return effects;

            int depth = 0;
            var current = new StringBuilder();

            foreach (char c in s)
            {
                if (c == '[') depth++;
                if (depth > 0) current.Append(c);
                if (c == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        var eff = ParseEffect(current.ToString().Trim());
                        eff.SpellId = spellId;
                        eff.SpellLevel = levelNum;
                        effects.Add(eff);
                        current.Clear();
                    }
                }
            }

            return effects;
        }

        private static SpellEffect ParseEffect(string s)
        {
            // s is like "[265, 7, null, null, 4, 0, 0d0+7]"
            s = s.Substring(1, s.Length - 2);
            var parts = s.Split(',');
            return new SpellEffect
            {
                Type = ParseInt(parts[0].Trim()),
                Value1 = ParseInt(parts[1].Trim()),
                Value2 = ParseInt(parts[2].Trim()),
                Value3 = ParseInt(parts[3].Trim()),
                Duration = ParseInt(parts[4].Trim()),
                Chance = ParseInt(parts[5].Trim()),
                // parts[6] = formula string (e.g. "1d5+1"), informational only
            };
        }

        private static List<string> SplitTopLevel(string s)
        {
            var result = new List<string>();
            int depth = 0;
            var current = new StringBuilder();

            foreach (char c in s)
            {
                if (c == '[') depth++;
                else if (c == ']') depth--;

                if (c == ',' && depth == 0)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                result.Add(current.ToString().Trim());

            return result;
        }

        private static int ParseInt(string s)
        {
            if (s == "null" || string.IsNullOrEmpty(s)) return 0;
            int.TryParse(s, out int v);
            return v;
        }

        private static bool ParseBool(string s) => s.Trim() == "true";

        private static List<int> ParseIntList(string s)
        {
            s = s.Trim();
            if (string.IsNullOrEmpty(s) || s == "[]" || s == "null")
                return new List<int>();
            s = s.Substring(1, s.Length - 2).Trim();
            if (string.IsNullOrEmpty(s))
                return new List<int>();
            return s.Split(',')
                .Select(x => { int.TryParse(x.Trim(), out int v); return v; })
                .ToList();
        }
    }
}
