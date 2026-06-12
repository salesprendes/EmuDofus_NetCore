using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

            foreach (var provider in m_providers.Reverse())
                if (provider.TryGet(key, out value))
                    return true;

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
                foreach (var field in type.GetFields())
                {
                    var attr = field.GetCustomAttribute<ConfigurableAttribute>();

                    if (attr == null)
                        continue;

                    if (attr.Name == string.Empty)
                        attr.Name = field.Name;

                    if (m_configurables.ContainsKey(attr.Name))
                        throw new Exception(string.Format("Configurable name's `{0}` is already used.", attr.Name));

                    m_configurables.Add(attr.Name, field);
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
                    configurable.Value.SetValue(null, value);
                }
            }
        }

        public void Commit()
        {
            var final = m_commitableProviders.LastOrDefault();

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

            if (configurationProvider is ICommitableProvider)
            {
                m_commitableProviders.Add(configurationProvider as ICommitableProvider);
            }
        }
    }
}
