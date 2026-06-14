using System;
using System.Reflection;

namespace Protocolo.Framework.Database
{
    public sealed partial class CustomPropertyTypeMap : SqlMapper.ITypeMap
    {
        private readonly Type _type;
        private readonly Func<Type, string, PropertyInfo> _propertySelector;

        public CustomPropertyTypeMap(Type type, Func<Type, string, PropertyInfo> propertySelector)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }

            _type = type;
            _propertySelector = propertySelector;
        }

        public ConstructorInfo FindConstructor(string[] names, Type[] types) => _type.GetConstructor(Array.Empty<Type>());
        public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName) => throw new NotSupportedException();

        public SqlMapper.IMemberMap GetMember(string columnName)
        {
            var prop = _propertySelector(_type, columnName);
            return prop != null ? new SimpleMemberMap(columnName, prop) : null;
        }
    }
}
