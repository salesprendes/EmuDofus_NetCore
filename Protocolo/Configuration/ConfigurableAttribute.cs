using System;

namespace Protocolo.Framework.Configuration
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigurableAttribute : Attribute
    {
        public string Name
        {
            get;
            set;
        }

        public ConfigurableAttribute(string name = "")
        {
            Name = name;
        }
    }
}
