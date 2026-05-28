using Game.Entity;
using System;
using System.Reflection;

namespace Game.Fight.AI.Core
{
    public static class AIProfileResolver
    {
        // ─── Template IDs confirmados en la DB (tabla monstruos_template) ──────────
        //
        // Usado como FALLBACK cuando el template no tiene columna ai_profile configurada.
        // Para añadir nuevos bosses:
        //   1. Confirmar el template ID en DB.
        //   2. Añadir el case en ResolveByTemplateId().
        //   3. Si es un nuevo AIProfile, añadirlo al enum y registrarlo en AIBrainFactory.
        //
        // ── Kralamar Gigante y tentáculos ──────────────────────────────────────────
        //   423  = Kralamoure Géant         → Kralamar
        //   424  = Tentacule Primaire        → KralamarTentacle
        //   425  = Tentacule Kralamoure      → KralamarTentacle
        //   1090 = Tentáculo Cuaternario     → KralamarTentacle (hechizo 1110)
        //   1091 = Tentáculo Terciario       → KralamarTentacle (hechizo 1109)
        //   1092 = Tentáculo Secundario      → KralamarTentacle (hechizo 1108)

        public static AIProfile Resolve(AIFighter fighter)
        {
            if (fighter == null)
                return AIProfile.Default;

            if (IsTaxCollector(fighter))
                return AIProfile.TaxCollector;

            var monster  = fighter as MonsterEntity;
            var template = monster?.Grade?.Template;

            // 1. Columna ai_profile/AiProfile del template (configuración explícita)
            var profile = ReadProfileValue(template);
            if (profile.HasValue)
                return profile.Value;

            // 2. Fallback por template ID (IDs confirmados en la DB del proyecto)
            if (monster != null)
            {
                var byId = ResolveByTemplateId(monster.Grade?.MonsterId ?? 0);
                if (byId.HasValue)
                    return byId.Value;
            }

            return AIProfile.Default;
        }

        private static bool IsTaxCollector(AIFighter fighter)
        {
            try
            {
                return fighter is TaxCollectorEntity;
            }
            catch
            {
                return false;
            }
        }

        private static AIProfile? ResolveByTemplateId(int monsterId)
        {
            switch (monsterId)
            {
                case 423:                              // Kralamoure Géant
                    return AIProfile.Kralamar;

                case 424:                              // Tentacule Primaire
                case 425:                              // Tentacule Kralamoure
                case 1090:                             // Tentáculo Cuaternario
                case 1091:                             // Tentáculo Terciario
                case 1092:                             // Tentáculo Secundario
                    return AIProfile.KralamarTentacle;

                // Añadir más bosses aquí cuando sus template IDs estén confirmados:
                // case XXX: return AIProfile.Kimbo;
                // case YYY: return AIProfile.Rasboul;

                default:
                    return null;
            }
        }

        private static AIProfile? ReadProfileValue(object template)
        {
            if (template == null)
                return null;

            foreach (var name in new[] { "AiProfile", "AIProfile", "ai_profile" })
            {
                var property = template.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (property == null)
                    continue;

                var value = property.GetValue(template, null);
                if (value == null)
                    continue;

                try
                {
                    var intValue = Convert.ToInt32(value);
                    if (intValue <= 0)
                        continue;

                    if (Enum.IsDefined(typeof(AIProfile), intValue))
                        return (AIProfile)intValue;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}
