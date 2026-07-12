using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Protocolo.Framework.Database
{
    public sealed partial class DefaultTypeMap : SqlMapper.ITypeMap
    {
        private static readonly ConcurrentDictionary<Type, TypeMembers> MemberCache = new ConcurrentDictionary<Type, TypeMembers>();

        private sealed class TypeMembers
        {
            internal List<FieldInfo> Fields;
            internal List<PropertyInfo> Properties;
        }

        private readonly Dictionary<string, FieldInfo> _fieldsByName;
        private readonly Dictionary<string, FieldInfo> _fieldsByNameIgnoreCase;
        private readonly Dictionary<string, PropertyInfo> _propertiesByName;
        private readonly Dictionary<string, PropertyInfo> _propertiesByNameIgnoreCase;
        private readonly Type _type;

        public DefaultTypeMap(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var fields = GetSettableFields(type);
            var properties = GetSettableProps(type);
            _fieldsByName = CreateLookup(fields, field => field.Name, StringComparer.Ordinal);
            _fieldsByNameIgnoreCase = CreateLookup(fields, field => field.Name, StringComparer.OrdinalIgnoreCase);
            _propertiesByName = CreateLookup(properties, property => property.Name, StringComparer.Ordinal);
            _propertiesByNameIgnoreCase = CreateLookup(properties, property => property.Name, StringComparer.OrdinalIgnoreCase);
            _type = type;
        }

        private static Dictionary<string, TMember> CreateLookup<TMember>(
            IEnumerable<TMember> members,
            Func<TMember, string> getName,
            StringComparer comparer)
        {
            var lookup = new Dictionary<string, TMember>(comparer);
            foreach (var member in members)
                lookup.TryAdd(getName(member), member);

            return lookup;
        }

        internal static MethodInfo GetPropertySetter(PropertyInfo propertyInfo, Type type)
        {
            return propertyInfo.DeclaringType == type ?
                propertyInfo.GetSetMethod(true) :
                propertyInfo.DeclaringType.GetProperty(propertyInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetSetMethod(true);
        }

        internal static List<PropertyInfo> GetSettableProps(Type t)
        {
            return GetMembers(t).Properties;
        }

        internal static List<FieldInfo> GetSettableFields(Type t)
        {
            return GetMembers(t).Fields;
        }

        private static TypeMembers GetMembers(Type type)
        {
            return MemberCache.GetOrAdd(type, static mappedType => new TypeMembers
            {
                Properties = mappedType
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(property => GetPropertySetter(property, mappedType) != null)
                    .ToList(),
                Fields = mappedType
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .ToList()
            });
        }

        public ConstructorInfo FindConstructor(string[] names, Type[] types)
        {
            var constructors = _type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (ConstructorInfo ctor in constructors.OrderBy(c => c.IsPublic ? 0 : (c.IsPrivate ? 2 : 1)).ThenBy(c => c.GetParameters().Length))
            {
                ParameterInfo[] ctorParameters = ctor.GetParameters();
                if (ctorParameters.Length == 0)
                    return ctor;

                if (ctorParameters.Length == types.Length)
                {
                    var parameterIndex = 0;
                    var parametersMatch = true;
                    while (parameterIndex < ctorParameters.Length && parametersMatch)
                    {
                        var parameter = ctorParameters[parameterIndex];
                        parametersMatch = string.Equals(parameter.Name, names[parameterIndex], StringComparison.OrdinalIgnoreCase);

                        if (parametersMatch && !(types[parameterIndex] == typeof(byte[]) && parameter.ParameterType.FullName == SqlMapper.LinqBinary))
                        {
                            var unboxedType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                            parametersMatch = unboxedType == types[parameterIndex]
                                || (unboxedType.IsEnum && Enum.GetUnderlyingType(unboxedType) == types[parameterIndex])
                                || (unboxedType == typeof(char) && types[parameterIndex] == typeof(string));
                        }

                        parameterIndex++;
                    }

                    if (parametersMatch)
                        return ctor;
                }
            }

            return null;
        }

        public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName)
        {
            var parameters = constructor.GetParameters();
            ParameterInfo matchingParameter = null;
            var parameterIndex = 0;
            while (parameterIndex < parameters.Length && matchingParameter == null)
            {
                var parameter = parameters[parameterIndex];
                if (string.Equals(parameter.Name, columnName, StringComparison.OrdinalIgnoreCase))
                    matchingParameter = parameter;

                parameterIndex++;
            }

            return matchingParameter == null ? null : new SimpleMemberMap(columnName, matchingParameter);
        }

        public SqlMapper.IMemberMap GetMember(string columnName)
        {
            if (!_propertiesByName.TryGetValue(columnName, out var property))
                _propertiesByNameIgnoreCase.TryGetValue(columnName, out property);

            if (property != null)
                return new SimpleMemberMap(columnName, property);

            if (!_fieldsByName.TryGetValue(columnName, out var field))
                _fieldsByNameIgnoreCase.TryGetValue(columnName, out field);

            return field == null ? null : new SimpleMemberMap(columnName, field);
        }
    }

}
