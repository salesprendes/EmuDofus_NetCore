using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Protocolo.Framework.Database
{
    public sealed partial class DefaultTypeMap : SqlMapper.ITypeMap
    {
        private readonly List<FieldInfo> _fields;
        private readonly List<PropertyInfo> _properties;
        private readonly Type _type;

        public DefaultTypeMap(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _fields = GetSettableFields(type);
            _properties = GetSettableProps(type);
            _type = type;
        }

        internal static MethodInfo GetPropertySetter(PropertyInfo propertyInfo, Type type)
        {
            return propertyInfo.DeclaringType == type ?
                propertyInfo.GetSetMethod(true) :
                propertyInfo.DeclaringType.GetProperty(propertyInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetSetMethod(true);
        }

        internal static List<PropertyInfo> GetSettableProps(Type t)
        {
            return t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(p => GetPropertySetter(p, t) != null).ToList();
        }

        internal static List<FieldInfo> GetSettableFields(Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();
        }

        public ConstructorInfo FindConstructor(string[] names, Type[] types)
        {
            var constructors = _type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (ConstructorInfo ctor in constructors.OrderBy(c => c.IsPublic ? 0 : (c.IsPrivate ? 2 : 1)).ThenBy(c => c.GetParameters().Length))
            {
                ParameterInfo[] ctorParameters = ctor.GetParameters();
                if (ctorParameters.Length == 0)
                {
                    return ctor;
                }

                if (ctorParameters.Length != types.Length)
                {
                    continue;
                }

                int i = 0;
                for (; i < ctorParameters.Length; i++)
                {
                    if (!string.Equals(ctorParameters[i].Name, names[i], StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (types[i] == typeof(byte[]) && ctorParameters[i].ParameterType.FullName == SqlMapper.LinqBinary)
                    {
                        continue;
                    }

                    var unboxedType = Nullable.GetUnderlyingType(ctorParameters[i].ParameterType) ?? ctorParameters[i].ParameterType;
                    if (unboxedType != types[i]
                        && !(unboxedType.IsEnum && Enum.GetUnderlyingType(unboxedType) == types[i])
                        && !(unboxedType == typeof(char) && types[i] == typeof(string)))
                    {
                        break;
                    }
                }

                if (i == ctorParameters.Length)
                {
                    return ctor;
                }
            }

            return null;
        }

        public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName)
        {
            var parameters = constructor.GetParameters();

            return new SimpleMemberMap(columnName, parameters.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase)));
        }

        public SqlMapper.IMemberMap GetMember(string columnName)
        {
            var property = _properties.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.Ordinal))
               ?? _properties.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (property != null)
            {
                return new SimpleMemberMap(columnName, property);
            }

            var field = _fields.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.Ordinal))
               ?? _fields.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (field != null)
            {
                return new SimpleMemberMap(columnName, field);
            }

            return null;
        }
    }

}
