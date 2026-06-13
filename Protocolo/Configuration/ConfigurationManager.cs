using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Protocolo.Framework.Configuration
{
    public class ConfigurationManager
    {
        private readonly IList<IConfigurationProvider> m_providers;
        private readonly IList<ICommitableProvider> m_commitableProviders;
        private readonly IDictionary<string, FieldInfo> m_configurables;

        public ConfigurationManager()
        {
            m_providers = new List<IConfigurationProvider>();
            m_commitableProviders = new List<ICommitableProvider>();
            m_configurables = new Dictionary<string, FieldInfo>();
        }

        public bool TryGet(string key, out object value)
        {
            if (key == null) throw new ArgumentNullException("key");

            for (var i = m_providers.Count - 1; i >= 0; i--)
            {
                var provider = m_providers[i];
                if (provider.TryGet(key, out value))
                    return true;
            }

            value = null;
            return false;
        }

        public void Set(string key, object value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            foreach (var provider in m_providers)
            {
                provider.Set(key, value);
            }
        }

        public void RegisterAttributes()
        {
            RegisterAttributes(Assembly.GetCallingAssembly());
        }

        public void RegisterAttributes(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            foreach (var type in assembly.GetTypes())
            {
                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attr = field.GetCustomAttribute<ConfigurableAttribute>();

                    if (attr == null)
                        continue;

                    var name = string.IsNullOrEmpty(attr.Name) ? field.Name : attr.Name;

                    if (m_configurables.ContainsKey(name))
                        throw new Exception(string.Format("Configurable name's `{0}` is already used.", name));

                    m_configurables.Add(name, field);
                }
            }
        }

        public void Load()
        {
            foreach (var configurable in m_configurables)
            {
                object value;
                if (TryGet(configurable.Key, out value))
                {
                    configurable.Value.SetValue(null, ConvertValue(value, configurable.Value.FieldType, configurable.Key));
                }
            }
        }

        public void Commit()
        {
            var final = m_commitableProviders.Count == 0 ? null : m_commitableProviders[m_commitableProviders.Count - 1];

            if (final == null)
                throw new InvalidOperationException("no commitable provider available");

            final.Commit();
        }

        public void Add(IConfigurationProvider configurationProvider, bool setAll = false)
        {
            if (setAll)
            {
                foreach (var configurable in m_configurables)
                {
                    configurationProvider.Set(configurable.Key, configurable.Value.GetValue(null));
                }
            }

            configurationProvider.Load();
            m_providers.Add(configurationProvider);

            if (configurationProvider is ICommitableProvider commitableProvider)
            {
                m_commitableProviders.Add(commitableProvider);
            }
        }

        private static object ConvertValue(object value, Type targetType, string key)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            var nullableType = Nullable.GetUnderlyingType(targetType);
            if (value == null)
            {
                if (!targetType.IsValueType || nullableType != null)
                    return null;

                throw new InvalidOperationException(string.Format("Configuration value `{0}` cannot be null for type `{1}`.", key, targetType.FullName));
            }

            var valueType = value.GetType();
            if (targetType.IsAssignableFrom(valueType))
                return value;

            var conversionType = nullableType ?? targetType;

            if (conversionType.IsEnum)
            {
                if (value is string enumName)
                    return Enum.Parse(conversionType, enumName, ignoreCase: true);

                var enumValue = Convert.ChangeType(value, Enum.GetUnderlyingType(conversionType), CultureInfo.InvariantCulture);
                return Enum.ToObject(conversionType, enumValue);
            }

            if (conversionType == typeof(string))
                return Convert.ToString(value, CultureInfo.InvariantCulture);

            return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }
    }
}
