using Protocolo.Framework.Generic;
using Game.Entity;
using Game.Spell;
using System;
using System.Collections.Generic;

namespace Game.Condition
{
    public sealed class ConditionParser : Singleton<ConditionParser>
    {
        private readonly Dictionary<string, Func<CharacterEntity, bool>> m_compiledExpressions;

        public ConditionParser()
        {
            m_compiledExpressions = new Dictionary<string, Func<CharacterEntity, bool>>();
        }

        public bool Check(string conditions, CharacterEntity character)
        {
            if (string.IsNullOrWhiteSpace(conditions))
                return true;

            Func<CharacterEntity, bool> method;
            lock (m_compiledExpressions)
            {
                if (!m_compiledExpressions.TryGetValue(conditions, out method))
                {
                    method = Compile(conditions);
                    m_compiledExpressions[conditions] = method;
                }
            }
            return method(character);
        }

        private static Func<CharacterEntity, bool> Compile(string conditions)
        {
            if (conditions.Contains("BI"))
                return _ => false;

            var orParts = conditions.Split('|');
            var orFuncs = new Func<CharacterEntity, bool>[orParts.Length];

            for (int i = 0; i < orParts.Length; i++)
            {
                var andParts = orParts[i].Split('&');
                var andFuncs = new List<Func<CharacterEntity, bool>>(andParts.Length);

                foreach (var part in andParts)
                {
                    var atom = ParseAtom(part.Trim());
                    if (atom != null)
                        andFuncs.Add(atom);
                }

                if (andFuncs.Count == 0)
                {
                    orFuncs[i] = _ => true;
                }
                else
                {
                    var captured = andFuncs.ToArray();
                    orFuncs[i] = ch =>
                    {
                        foreach (var f in captured)
                            if (!f(ch)) return false;
                        return true;
                    };
                }
            }

            if (orFuncs.Length == 1)
                return orFuncs[0];

            return ch =>
            {
                foreach (var f in orFuncs)
                    if (f(ch)) return true;
                return false;
            };
        }

        private static Func<CharacterEntity, bool> ParseAtom(string expr)
        {
            if (string.IsNullOrEmpty(expr))
                return null;

            // Inventory template checks use bool-returning methods
            if (expr.StartsWith("PO==", StringComparison.Ordinal))
            {
                if (int.TryParse(expr.AsSpan(4), out int id))
                    return ch => ch.Inventory.HasTemplate(id);
                return null;
            }
            if (expr.StartsWith("PO!=", StringComparison.Ordinal))
            {
                if (int.TryParse(expr.AsSpan(4), out int id))
                    return ch => ch.Inventory.NotHasTemplate(id);
                return null;
            }

            // Detect operator — multi-char first to avoid partial matches on > or <
            string op = null;
            int opIdx = -1;

            foreach (var candidate in new[] { ">=", "<=", "!=", "==" })
            {
                int idx = expr.IndexOf(candidate, StringComparison.Ordinal);
                if (idx >= 0) { op = candidate; opIdx = idx; break; }
            }

            if (op == null)
            {
                foreach (var candidate in new[] { ">", "<" })
                {
                    int idx = expr.IndexOf(candidate, StringComparison.Ordinal);
                    if (idx >= 0) { op = candidate; opIdx = idx; break; }
                }
            }

            if (op == null || opIdx <= 0) return null;

            var code = expr.AsSpan(0, opIdx);
            var valueSpan = expr.AsSpan(opIdx + op.Length);

            if (!long.TryParse(valueSpan, out long value)) return null;

            var getter = GetValueGetter(code);
            if (getter == null) return null;

            switch (op)
            {
                case "==": return ch => getter(ch) == value;
                case "!=": return ch => getter(ch) != value;
                case ">":  return ch => getter(ch) >  value;
                case "<":  return ch => getter(ch) <  value;
                case ">=": return ch => getter(ch) >= value;
                case "<=": return ch => getter(ch) <= value;
                default:   return null;
            }
        }

        private static Func<CharacterEntity, long> GetValueGetter(ReadOnlySpan<char> code)
        {
            return code switch
            {
                // Stats totales
                "CI" => ch => ch.Statistics.GetTotal(EffectEnum.AddIntelligence),
                "CV" => ch => ch.Statistics.GetTotal(EffectEnum.AddVitality),
                "CA" => ch => ch.Statistics.GetTotal(EffectEnum.AddAgility),
                "CW" => ch => ch.Statistics.GetTotal(EffectEnum.AddWisdom),
                "CC" => ch => ch.Statistics.GetTotal(EffectEnum.AddChance),
                "CS" => ch => ch.Statistics.GetTotal(EffectEnum.AddStrength),
                // Stats base
                "Ci" => ch => ch.DatabaseRecord.Intelligence,
                "Cs" => ch => ch.DatabaseRecord.Strength,
                "Cv" => ch => ch.DatabaseRecord.Vitality,
                "Ca" => ch => ch.DatabaseRecord.Agility,
                "Cw" => ch => ch.DatabaseRecord.Wisdom,
                "Cc" => ch => ch.DatabaseRecord.Chance,
                // Personaje
                "Ps" => ch => ch.AlignmentId,
                "Pa" => ch => ch.AlignmentPromotion,
                "PP" => ch => ch.AlignmentLevel,
                "PL" => ch => ch.Level,
                "PK" => ch => ch.Inventory.Kamas,
                "PG" => ch => ch.BreedId,
                "PS" => ch => ch.Sex,
                "PZ" => ch => 1,     // Suscriptor (siempre true)
                "PJ" => ch => 0,     // HasJob
                "MK" => ch => 0,     // HasJob
                "Pg" => ch => 0,     // Don
                "PR" => ch => 0,     // Married
                "PX" => ch => ch.Account.Power,
                "PW" => ch => 10000, // MaxWeight
                "PB" => ch => ch.Map.SubAreaId,
                "SI" => ch => ch.MapId,
                "MiS" => ch => ch.Id,
                _ => null,
            };
        }
    }
}
