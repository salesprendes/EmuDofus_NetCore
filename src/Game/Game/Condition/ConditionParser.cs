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
                if (int.TryParse(expr.Substring(4), out int id))
                    return ch => ch.Inventory.HasTemplate(id);
                return null;
            }
            if (expr.StartsWith("PO!=", StringComparison.Ordinal))
            {
                if (int.TryParse(expr.Substring(4), out int id))
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

            var code = expr.Substring(0, opIdx);
            var valueStr = expr.Substring(opIdx + op.Length);

            if (!long.TryParse(valueStr, out long value)) return null;

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

        private static Func<CharacterEntity, long> GetValueGetter(string code)
        {
            switch (code)
            {
                // Stats totales
                case "CI":  return ch => ch.Statistics.GetTotal(EffectEnum.AddIntelligence);
                case "CV":  return ch => ch.Statistics.GetTotal(EffectEnum.AddVitality);
                case "CA":  return ch => ch.Statistics.GetTotal(EffectEnum.AddAgility);
                case "CW":  return ch => ch.Statistics.GetTotal(EffectEnum.AddWisdom);
                case "CC":  return ch => ch.Statistics.GetTotal(EffectEnum.AddChance);
                case "CS":  return ch => ch.Statistics.GetTotal(EffectEnum.AddStrength);
                // Stats base
                case "Ci":  return ch => ch.DatabaseRecord.Intelligence;
                case "Cs":  return ch => ch.DatabaseRecord.Strength;
                case "Cv":  return ch => ch.DatabaseRecord.Vitality;
                case "Ca":  return ch => ch.DatabaseRecord.Agility;
                case "Cw":  return ch => ch.DatabaseRecord.Wisdom;
                case "Cc":  return ch => ch.DatabaseRecord.Chance;
                // Personaje
                case "Ps":  return ch => ch.AlignmentId;
                case "Pa":  return ch => ch.AlignmentPromotion;
                case "PP":  return ch => ch.AlignmentLevel;
                case "PL":  return ch => ch.Level;
                case "PK":  return ch => ch.Inventory.Kamas;
                case "PG":  return ch => ch.BreedId;
                case "PS":  return ch => ch.Sex;
                case "PZ":  return ch => 1;     // Suscriptor (siempre true)
                case "PJ":  return ch => 0;     // HasJob
                case "MK":  return ch => 0;     // HasJob
                case "Pg":  return ch => 0;     // Don
                case "PR":  return ch => 0;     // Married
                case "PX":  return ch => ch.Account.Power;
                case "PW":  return ch => 10000; // MaxWeight
                case "PB":  return ch => ch.Map.SubAreaId;
                case "SI":  return ch => ch.MapId;
                case "MiS": return ch => ch.Id;
                default:    return null;
            }
        }
    }
}
