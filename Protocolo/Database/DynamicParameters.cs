using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Protocolo.Framework.Database
{
    public partial class DynamicParameters : SqlMapper.IDynamicParameters
    {
        internal const DbType EnumerableMultiParameter = (DbType)(-1);
        private const int PARAM_READER_CACHE_MAX_ITEMS = 1024;

        static readonly Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> paramReaderCache = new Dictionary<SqlMapper.Identity, Action<IDbCommand, object>>();

        readonly Dictionary<string, ParamInfo> parameters = new Dictionary<string, ParamInfo>();
        List<object> templates;

        internal static void PurgeCache()
        {
            lock (paramReaderCache)
            {
                paramReaderCache.Clear();
            }
        }

        partial class ParamInfo
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public ParameterDirection ParameterDirection { get; set; }
            public DbType? DbType { get; set; }
            public int? Size { get; set; }
            public IDbDataParameter AttachedParam { get; set; }
        }

        /// <summary>
        /// construct a dynamic parameter bag
        /// </summary>
        public DynamicParameters() { }

        /// <summary>
        /// construct a dynamic parameter bag
        /// </summary>
        /// <param name="template">can be an anonymous type or a DynamicParameters bag</param>
        public DynamicParameters(object template)
        {
            AddDynamicParams(template);
        }

        /// <summary>
        /// Append a whole object full of params to the dynamic
        /// EG: AddDynamicParams(new {A = 1, B = 2}) // will add property A and B to the dynamic
        /// </summary>
        /// <param name="param"></param>
        public void AddDynamicParams(dynamic param)
        {
            var obj = (object)param;
            if (obj != null)
            {
                var subDynamic = obj as DynamicParameters;
                if (subDynamic == null)
                {
                    var dictionary = obj as IEnumerable<KeyValuePair<string, object>>;
                    if (dictionary == null)
                    {
                        templates = templates ?? new List<object>();
                        templates.Add(obj);
                    }
                    else
                    {
                        foreach (var kvp in dictionary)
                        {
                            Add(kvp.Key, kvp.Value);
                        }
                    }
                }
                else
                {
                    if (subDynamic.parameters != null)
                    {
                        foreach (var kvp in subDynamic.parameters)
                        {
                            parameters.Add(kvp.Key, kvp.Value);
                        }
                    }

                    if (subDynamic.templates != null)
                    {
                        templates = templates ?? new List<object>();
                        foreach (var t in subDynamic.templates)
                        {
                            templates.Add(t);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a parameter to this dynamic parameter list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        /// <param name="direction"></param>
        /// <param name="size"></param>
        public void Add(string name, object value = null, DbType? dbType = null, ParameterDirection? direction = null, int? size = null)
        {
            parameters[Clean(name)] = new ParamInfo
            {
                Name = name,
                Value = value,
                ParameterDirection = direction ?? ParameterDirection.Input,
                DbType = dbType,
                Size = size
            };
        }

        static string Clean(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                switch (name[0])
                {
                    case '@':
                    case ':':
                    case '?':
                        return name.Substring(1);
                }
            }
            return name;
        }

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            AddParameters(command, identity);
        }

        /// <summary>
        /// Add all the parameters needed to the command just before it executes
        /// </summary>
        /// <param name="command">The raw command prior to execution</param>
        /// <param name="identity">Information about the query</param>
        protected void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            if (templates != null)
            {
                foreach (var template in templates)
                {
                    var newIdent = identity.ForDynamicParameters(template.GetType());
                    Action<IDbCommand, object> appender;

                    lock (paramReaderCache)
                    {
                        if (!paramReaderCache.TryGetValue(newIdent, out appender))
                        {
                            if (paramReaderCache.Count >= PARAM_READER_CACHE_MAX_ITEMS)
                            {
                                paramReaderCache.Clear();
                            }

                            appender = SqlMapper.CreateParamInfoGenerator(newIdent, true);
                            paramReaderCache[newIdent] = appender;
                        }
                    }

                    appender(command, template);
                }
            }

            foreach (var param in parameters.Values)
            {
                var dbType = param.DbType;
                var val = param.Value;
                string name = Clean(param.Name);

                if (dbType == null && val != null)
                {
                    dbType = SqlMapper.LookupDbType(val.GetType(), name);
                }

                if (dbType == DynamicParameters.EnumerableMultiParameter)
                {
                    SqlMapper.PackListParameters(command, name, val);
                }
                else
                {

                    var add = !command.Parameters.Contains(name);
                    IDbDataParameter p;
                    if (add)
                    {
                        p = command.CreateParameter();
                        p.ParameterName = name;
                    }
                    else
                    {
                        p = (IDbDataParameter)command.Parameters[name];
                    }

                    p.Value = val ?? DBNull.Value;
                    p.Direction = param.ParameterDirection;
                    if (val is string s)
                    {
                        if (s.Length <= 4000)
                        {
                            p.Size = 4000;
                        }
                    }
                    if (param.Size != null)
                    {
                        p.Size = param.Size.Value;
                    }
                    if (dbType != null)
                    {
                        p.DbType = dbType.Value;
                    }
                    if (add)
                    {
                        command.Parameters.Add(p);
                    }
                    param.AttachedParam = p;
                }

            }
        }

        /// <summary>
        /// All the names of the param in the bag, use Get to yank them out
        /// </summary>
        public IEnumerable<string> ParameterNames
        {
            get
            {
                return parameters.Keys;
            }
        }


        /// <summary>
        /// Get the value of a parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns>The value, note DBNull.Value is not returned, instead the value is returned as null</returns>
        public T Get<T>(string name)
        {
            var val = parameters[Clean(name)].AttachedParam.Value;
            if (val == DBNull.Value)
            {
                if (default(T) != null)
                {
                    throw new ApplicationException("Attempting to cast a DBNull to a non nullable type!");
                }
                return default(T);
            }
            return (T)val;
        }
    }
}
