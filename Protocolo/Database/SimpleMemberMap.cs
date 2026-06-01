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

        /// <summary>
        /// Creates instance for simple property mapping
        /// </summary>
        /// <param name="columnName">DataReader column name</param>
        /// <param name="property">Target property</param>
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

        /// <summary>
        /// Creates instance for simple field mapping
        /// </summary>
        /// <param name="columnName">DataReader column name</param>
        /// <param name="field">Target field</param>
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

        /// <summary>
        /// Creates instance for simple constructor parameter mapping
        /// </summary>
        /// <param name="columnName">DataReader column name</param>
        /// <param name="parameter">Target constructor parameter</param>
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

        /// <summary>
        /// DataReader column name
        /// </summary>
        public string ColumnName
        {
            get { return _columnName; }
        }

        /// <summary>
        /// Target member type
        /// </summary>
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

        /// <summary>
        /// Target property
        /// </summary>
        public PropertyInfo Property
        {
            get { return _property; }
        }

        /// <summary>
        /// Target field
        /// </summary>
        public FieldInfo Field
        {
            get { return _field; }
        }

        /// <summary>
        /// Target constructor parameter
        /// </summary>
        public ParameterInfo Parameter
        {
            get { return _parameter; }
        }
    }

    /// <summary>
    /// Represents default type mapping strategy used by Dapper
    /// </summary>
}
