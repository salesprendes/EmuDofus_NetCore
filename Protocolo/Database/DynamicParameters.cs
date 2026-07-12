using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace Protocolo.Framework.Database
{
    public partial class DynamicParameters : SqlMapper.IDynamicParameters
    {
        internal const DbType EnumerableMultiParameter = (DbType)(-1);
        private const int PARAM_READER_CACHE_MAX_ITEMS = 1024;

        private static readonly ConcurrentDictionary<SqlMapper.Identity, Lazy<Action<IDbCommand, object>>> ParamReaderCache = new ConcurrentDictionary<SqlMapper.Identity, Lazy<Action<IDbCommand, object>>>();
        private static int m_purgingCache;

        readonly Dictionary<string, ParamInfo> parameters = new Dictionary<string, ParamInfo>();
        List<object> templates;

        internal static void PurgeCache()
        {
            ParamReaderCache.Clear();
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

        public DynamicParameters() { }

        public DynamicParameters(object template)
        {
            AddDynamicParams(template);
        }

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

        public void Add(string name, object value = null, DbType? dbType = null, ParameterDirection? direction = null, int? size = null)
        {
            var cleanName = Clean(name);
            parameters[cleanName] = new ParamInfo { Name = cleanName, Value = value, ParameterDirection = direction ?? ParameterDirection.Input, DbType = dbType, Size = size };
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
                        return name.AsSpan(1).ToString();
                }
            }
            return name;
        }

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            AddParameters(command, identity);
        }

        protected void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            if (templates != null)
            {
                foreach (var template in templates)
                {
                    var newIdent = identity.ForDynamicParameters(template.GetType());
                    if (ParamReaderCache.Count >= PARAM_READER_CACHE_MAX_ITEMS && Interlocked.CompareExchange(ref m_purgingCache, 1, 0) == 0)
                    {
                        ParamReaderCache.Clear();
                        Volatile.Write(ref m_purgingCache, 0);
                    }

                    var appender = ParamReaderCache.GetOrAdd(
                        newIdent,
                        static cacheIdentity => new Lazy<Action<IDbCommand, object>>(
                            () => SqlMapper.CreateParamInfoGenerator(cacheIdentity, true),
                            LazyThreadSafetyMode.ExecutionAndPublication)).Value;

                    appender(command, template);
                }
            }

            foreach (var param in parameters.Values)
            {
                var dbType = param.DbType;
                var val = param.Value;
                string name = param.Name;

                if (dbType == null && val != null)
                {
                    dbType = SqlMapper.LookupDbType(val.GetType(), name);
                }

                if (dbType == EnumerableMultiParameter)
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

        public IEnumerable<string> ParameterNames
        {
            get
            {
                return parameters.Keys;
            }
        }


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
