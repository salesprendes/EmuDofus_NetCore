using Game.Entity;
using System;
using System.Reflection;

namespace Game.Fight.AI.Core
{
    public static class AIProfileResolver
    {
















        public static AIProfile Resolve(AIFighter fighter)
        {
            if (fighter == null)
                return AIProfile.Default;

            if (IsTaxCollector(fighter))
                return AIProfile.TaxCollector;

            var monster = fighter as MonsterEntity;
            var template = monster?.Grade?.Template;


            var profile = ReadProfileValue(template);
            if (profile.HasValue)
                return profile.Value;


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
                case 423:
                    return AIProfile.Kralamar;

                case 424:
                case 425:
                case 1090:
                case 1091:
                case 1092:
                    return AIProfile.KralamarTentacle;





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
