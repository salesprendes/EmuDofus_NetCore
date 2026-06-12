using System;
using System.Reflection;

namespace Protocolo.Framework.Database
{
    public sealed partial class SimpleMemberMap : SqlMapper.IMemberMap
    {
        private readonly string _columnName;
        private readonly PropertyInfo _property;
        private readonly FieldInfo _field;
        private readonly ParameterInfo _parameter;

        public SimpleMemberMap(string columnName, PropertyInfo property)
        {
            if (columnName == null)
            {
                throw new ArgumentNullException(nameof(columnName));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            _columnName = columnName;
            _property = property;
        }

        public SimpleMemberMap(string columnName, FieldInfo field)
        {
            if (columnName == null)
            {
                throw new ArgumentNullException(nameof(columnName));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            _columnName = columnName;
            _field = field;
        }

        public SimpleMemberMap(string columnName, ParameterInfo parameter)
        {
            if (columnName == null)
            {
                throw new ArgumentNullException(nameof(columnName));
            }

            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            _columnName = columnName;
            _parameter = parameter;
        }

        public string ColumnName
        {
            get { return _columnName; }
        }

        public Type MemberType
        {
            get
            {
                if (_field != null)
                {
                    return _field.FieldType;
                }

                if (_property != null)
                {
                    return _property.PropertyType;
                }

                if (_parameter != null)
                {
                    return _parameter.ParameterType;
                }

                return null;
            }
        }

        public PropertyInfo Property
        {
            get { return _property; }
        }

        public FieldInfo Field
        {
            get { return _field; }
        }

        public ParameterInfo Parameter
        {
            get { return _parameter; }
        }
    }

}
