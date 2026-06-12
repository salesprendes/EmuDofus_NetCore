using Game.Database.Repository;
using Game.Database.Structure;
using Game.Spell;
using Protocolo.Framework.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            Logger.Info("SpellManager: " + m_templateById.Count + " hechizos cargados.");
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



        private static List<int> ParseTargets(string afectados)
        {
            if (string.IsNullOrEmpty(afectados))
                return new List<int>();
            return afectados.Split('|').Select(x => { int.TryParse(x.Trim(), out int v); return v; }).ToList();
        }

        private static SpellLevel ParseLevel(string s, int spellId, int levelNum)
        {
            if (string.IsNullOrEmpty(s) || s.Trim() == "-1" || s.Trim() == "[]")
                return null;

            s = s.Trim();
            if (s.Length < 2 || s[0] != '[')
                return null;

            var parts = SplitTopLevel(s.AsSpan(1, s.Length - 2));
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


            var data = s.AsSpan(1, s.Length - 2).Trim();
            if (data.IsEmpty)
                return effects;

            int depth = 0;
            int effectStart = -1;

            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];
                if (c == '[')
                {
                    if (depth == 0)
                        effectStart = i;

                    depth++;
                }

                if (c == ']')
                {
                    depth--;
                    if (depth == 0 && effectStart >= 0)
                    {
                        var eff = ParseEffect(data.Slice(effectStart, i - effectStart + 1).Trim());
                        eff.SpellId = spellId;
                        eff.SpellLevel = levelNum;
                        effects.Add(eff);
                        effectStart = -1;
                    }
                }
            }

            return effects;
        }

        private static SpellEffect ParseEffect(ReadOnlySpan<char> s)
        {

            var parts = s.Slice(1, s.Length - 2);
            Span<Range> effectParts = stackalloc Range[7];
            parts.Split(effectParts, ',');
            return new SpellEffect
            {
                Type = ParseInt(parts[effectParts[0]]),
                Value1 = ParseInt(parts[effectParts[1]]),
                Value2 = ParseInt(parts[effectParts[2]]),
                Value3 = ParseInt(parts[effectParts[3]]),
                Duration = ParseInt(parts[effectParts[4]]),
                Chance = ParseInt(parts[effectParts[5]]),

            };
        }

        private static List<string> SplitTopLevel(ReadOnlySpan<char> s)
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
            return ParseInt(s.AsSpan());
        }

        private static int ParseInt(ReadOnlySpan<char> s)
        {
            s = s.Trim();
            if (s.IsEmpty || s.Equals("null", StringComparison.Ordinal)) return 0;
            int.TryParse(s, out int v);
            return v;
        }

        private static bool ParseBool(string s) => s.AsSpan().Trim().Equals("true", StringComparison.Ordinal);

        private static List<int> ParseIntList(string s)
        {
            var result = new List<int>();
            var data = s.AsSpan().Trim();
            if (data.IsEmpty || data.Equals("[]", StringComparison.Ordinal) || data.Equals("null", StringComparison.Ordinal))
                return result;

            data = data.Slice(1, data.Length - 2).Trim();
            if (data.IsEmpty)
                return result;

            while (!data.IsEmpty)
            {
                var separatorIndex = data.IndexOf(',');
                var value = separatorIndex < 0 ? data : data.Slice(0, separatorIndex);
                result.Add(ParseInt(value));

                if (separatorIndex < 0)
                    break;

                data = data.Slice(separatorIndex + 1);
                if (data.IsEmpty)
                {
                    result.Add(0);
                    break;
                }
            }

            return result;
        }
    }
}
