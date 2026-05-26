using System;

namespace Protocolo.Framework.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigurableAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public ConfigurableAttribute(string name = "")
        {
            Name = name;
        }
    }
}
