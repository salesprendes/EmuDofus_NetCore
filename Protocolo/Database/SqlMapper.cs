using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Protocolo.Framework.Database
{
    public static partial class SqlMapper
    {
        public partial interface IDynamicParameters
        {
            void AddParameters(IDbCommand command, Identity identity);
        }

        public interface ICustomQueryParameter
        {
            void AddParameter(IDbCommand command, string name);
        }

        public interface ITypeMap
        {
            ConstructorInfo FindConstructor(string[] names, Type[] types);

            IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName);

            IMemberMap GetMember(string columnName);
        }

        public interface IMemberMap
        {
            string ColumnName { get; }

            Type MemberType { get; }

            PropertyInfo Property { get; }

            FieldInfo Field { get; }

            ParameterInfo Parameter { get; }
        }

        static Link<Type, Action<IDbCommand, bool>> bindByNameCache;
        static Action<IDbCommand, bool> GetBindByName(Type commandType)
        {
            if (commandType == null)
            {
                return null;
            }

            Action<IDbCommand, bool> action;
            if (Link<Type, Action<IDbCommand, bool>>.TryGet(bindByNameCache, commandType, out action))
            {
                return action;
            }
            var prop = commandType.GetProperty("BindByName", BindingFlags.Public | BindingFlags.Instance);
            action = null;
            ParameterInfo[] indexers;
            MethodInfo setter;
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool)
                && ((indexers = prop.GetIndexParameters()) == null || indexers.Length == 0)
                && (setter = prop.GetSetMethod()) != null
                )
            {
                var method = new DynamicMethod(commandType.Name + "_BindByName", null, new[] { typeof(IDbCommand), typeof(bool) });
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, commandType);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Callvirt, setter, null);
                il.Emit(OpCodes.Ret);
                action = (Action<IDbCommand, bool>)method.CreateDelegate(typeof(Action<IDbCommand, bool>));
            }

            Link<Type, Action<IDbCommand, bool>>.TryAdd(ref bindByNameCache, commandType, ref action);
            return action;
        }

        partial class Link<TKey, TValue> where TKey : class
        {
            public static bool TryGet(Link<TKey, TValue> link, TKey key, out TValue value)
            {
                while (link != null)
                {
                    if ((object)key == (object)link.Key)
                    {
                        value = link.Value;
                        return true;
                    }
                    link = link.Tail;
                }
                value = default;
                return false;
            }

            public static bool TryAdd(ref Link<TKey, TValue> head, TKey key, ref TValue value)
            {
                bool tryAgain;
                do
                {
                    var snapshot = Interlocked.CompareExchange(ref head, null, null);
                    TValue found;
                    if (TryGet(snapshot, key, out found))
                    {
                        value = found;
                        return false;
                    }
                    var newNode = new Link<TKey, TValue>(key, value, snapshot);
                    tryAgain = Interlocked.CompareExchange(ref head, newNode, snapshot) != snapshot;
                } while (tryAgain);
                return true;
            }
            private Link(TKey key, TValue value, Link<TKey, TValue> tail)
            {
                Key = key;
                Value = value;
                Tail = tail;
            }
            public TKey Key { get; private set; }
            public TValue Value { get; private set; }
            public Link<TKey, TValue> Tail { get; private set; }
        }
        partial class CacheInfo
        {
            public DeserializerState Deserializer { get; set; }
            public Func<IDataReader, object>[] OtherDeserializers { get; set; }
            public Action<IDbCommand, object> ParamReader { get; set; }
            private int hitCount;

            public int GetHitCount() { return Interlocked.CompareExchange(ref hitCount, 0, 0); }
            public int ResetHitCount() { return Interlocked.Exchange(ref hitCount, 0); }
            public void RecordHit() { Interlocked.Increment(ref hitCount); }
        }
        static int GetColumnHash(IDataReader reader)
        {
            unchecked
            {
                int colCount = reader.FieldCount, hash = colCount;
                for (int i = 0; i < colCount; i++)
                {
                    object tmp = reader.GetName(i);
                    hash = (hash * 31) + (tmp == null ? 0 : tmp.GetHashCode());
                }
                return hash;
            }
        }
        struct DeserializerState
        {
            public readonly int Hash;
            public readonly Func<IDataReader, object> Func;

            public DeserializerState(int hash, Func<IDataReader, object> func)
            {
                Hash = hash;
                Func = func;
            }
        }

        public static event EventHandler QueryCachePurged;

        private static void OnQueryCachePurged()
        {
            QueryCachePurged?.Invoke(null, EventArgs.Empty);
        }

        static readonly ConcurrentDictionary<Identity, CacheInfo> _queryCache = new ConcurrentDictionary<Identity, CacheInfo>();

        private static void SetQueryCache(Identity key, CacheInfo value)
        {
            if (Interlocked.Increment(ref collect) >= COLLECT_PER_ITEMS || _queryCache.Count >= QUERY_CACHE_MAX_ITEMS)
            {
                CollectCacheGarbage();
            }

            _queryCache[key] = value;
        }

        private static void CollectCacheGarbage()
        {
            try
            {
                var cacheCount = _queryCache.Count;
                foreach (var pair in _queryCache)
                {
                    var hits = pair.Value.ResetHitCount();
                    if (hits <= COLLECT_HIT_COUNT_MIN || (cacheCount > QUERY_CACHE_MAX_ITEMS && hits <= COLLECT_HIT_COUNT_KEEP_MIN))
                    {
                        CacheInfo cache;
                        if (_queryCache.TryRemove(pair.Key, out cache))
                        {
                            cacheCount--;
                        }
                    }
                }
            }

            finally
            {
                Interlocked.Exchange(ref collect, 0);
            }
        }

        private const int COLLECT_PER_ITEMS = 1000;
        private const int COLLECT_HIT_COUNT_MIN = 0;
        private const int COLLECT_HIT_COUNT_KEEP_MIN = 1;
        private const int QUERY_CACHE_MAX_ITEMS = 4096;
        private static int collect;
        private static bool TryGetQueryCache(Identity key, out CacheInfo value)
        {
            if (_queryCache.TryGetValue(key, out value))
            {
                value.RecordHit();
                return true;
            }
            value = null;
            return false;
        }

        public static void PurgeQueryCache()
        {
            _queryCache.Clear();
            DynamicParameters.PurgeCache();
            OnQueryCachePurged();
        }

        private static void PurgeQueryCacheByType(Type type)
        {
            foreach (var entry in _queryCache)
            {
                CacheInfo cache;
                if (entry.Key.type == type)
                {
                    _queryCache.TryRemove(entry.Key, out cache);
                }
            }
        }

        public static int GetCachedSQLCount()
        {
            return _queryCache.Count;
        }

        public static IEnumerable<Tuple<string, string, int>> GetCachedSQL(int ignoreHitCountAbove = int.MaxValue)
        {
            var data = _queryCache.Select(pair => Tuple.Create(pair.Key.connectionString, pair.Key.sql, pair.Value.GetHitCount()));
            if (ignoreHitCountAbove < int.MaxValue)
            {
                data = data.Where(tuple => tuple.Item3 <= ignoreHitCountAbove);
            }

            return data;
        }

        public static IEnumerable<Tuple<int, int>> GetHashCollisions()
        {
            var counts = new Dictionary<int, int>();
            foreach (var key in _queryCache.Keys)
            {
                int count;
                if (!counts.TryGetValue(key.hashCode, out count))
                {
                    counts.Add(key.hashCode, 1);
                }
                else
                {
                    counts[key.hashCode] = count + 1;
                }
            }
            return from pair in counts where pair.Value > 1 select Tuple.Create(pair.Key, pair.Value);
        }

        public static IEnumerable<Tuple<int, int>> GetHashCollissions()
        {
            return GetHashCollisions();
        }

        static readonly Dictionary<Type, DbType> typeMap = new Dictionary<Type, DbType>
        {
            { typeof(byte), DbType.Byte },
            { typeof(sbyte), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(int), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(float), DbType.Single },
            { typeof(double), DbType.Double },
            { typeof(decimal), DbType.Decimal },
            { typeof(bool), DbType.Boolean },
            { typeof(string), DbType.String },
            { typeof(char), DbType.StringFixedLength },
            { typeof(Guid), DbType.Guid },
            { typeof(DateTime), DbType.DateTime },
            { typeof(DateTimeOffset), DbType.DateTimeOffset },
            { typeof(TimeSpan), DbType.Time },
            { typeof(byte[]), DbType.Binary },
            { typeof(byte?), DbType.Byte },
            { typeof(sbyte?), DbType.SByte },
            { typeof(short?), DbType.Int16 },
            { typeof(ushort?), DbType.UInt16 },
            { typeof(int?), DbType.Int32 },
            { typeof(uint?), DbType.UInt32 },
            { typeof(long?), DbType.Int64 },
            { typeof(ulong?), DbType.UInt64 },
            { typeof(float?), DbType.Single },
            { typeof(double?), DbType.Double },
            { typeof(decimal?), DbType.Decimal },
            { typeof(bool?), DbType.Boolean },
            { typeof(char?), DbType.StringFixedLength },
            { typeof(Guid?), DbType.Guid },
            { typeof(DateTime?), DbType.DateTime },
            { typeof(DateTimeOffset?), DbType.DateTimeOffset },
            { typeof(TimeSpan?), DbType.Time },
            { typeof(object), DbType.Object },
        };

        public static void AddTypeMap(Type type, DbType dbType)
        {
            typeMap[type] = dbType;
        }

        internal const string LinqBinary = "System.Data.Linq.Binary";
        internal static DbType LookupDbType(Type type, string name)
        {
            DbType dbType;
            var nullUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullUnderlyingType != null)
            {
                type = nullUnderlyingType;
            }

            if (type.IsEnum && !typeMap.ContainsKey(type))
            {
                type = Enum.GetUnderlyingType(type);
            }
            if (typeMap.TryGetValue(type, out dbType))
            {
                return dbType;
            }
            if (type.FullName == LinqBinary)
            {
                return DbType.Binary;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return DynamicParameters.EnumerableMultiParameter;
            }


            throw new NotSupportedException(string.Format("The member {0} of type {1} cannot be used as a parameter value", name, type));
        }


        public partial class Identity : IEquatable<Identity>
        {
            internal Identity ForGrid(Type primaryType, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, null, gridIndex);
            }

            internal Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex)
            {
                return new Identity(sql, commandType, connectionString, primaryType, parametersType, otherTypes, gridIndex);
            }
            public Identity ForDynamicParameters(Type type)
            {
                return new Identity(sql, commandType, connectionString, this.type, type, null, -1);
            }

            internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type type, Type parametersType, Type[] otherTypes)
                : this(sql, commandType, connection.ConnectionString, type, parametersType, otherTypes, 0)
            { }
            private Identity(string sql, CommandType? commandType, string connectionString, Type type, Type parametersType, Type[] otherTypes, int gridIndex)
            {
                this.sql = sql;
                this.commandType = commandType;
                this.connectionString = connectionString;
                this.type = type;
                this.parametersType = parametersType;
                this.gridIndex = gridIndex;
                unchecked
                {
                    hashCode = 17;
                    hashCode = hashCode * 23 + commandType.GetHashCode();
                    hashCode = hashCode * 23 + gridIndex.GetHashCode();
                    hashCode = hashCode * 23 + (sql == null ? 0 : sql.GetHashCode());
                    hashCode = hashCode * 23 + (type == null ? 0 : type.GetHashCode());
                    if (otherTypes != null)
                    {
                        foreach (var t in otherTypes)
                        {
                            hashCode = hashCode * 23 + (t == null ? 0 : t.GetHashCode());
                        }
                    }
                    hashCode = hashCode * 23 + (connectionString == null ? 0 : SqlMapper.connectionStringComparer.GetHashCode(connectionString));
                    hashCode = hashCode * 23 + (parametersType == null ? 0 : parametersType.GetHashCode());
                }
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Identity);
            }
            public readonly string sql;
            public readonly CommandType? commandType;

            public readonly int hashCode, gridIndex;
            public readonly Type type;
            public readonly string connectionString;
            public readonly Type parametersType;
            public override int GetHashCode()
            {
                return hashCode;
            }
            public bool Equals(Identity other)
            {
                return
                    other != null &&
                    gridIndex == other.gridIndex &&
                    type == other.type &&
                    sql == other.sql &&
                    commandType == other.commandType &&
                    SqlMapper.connectionStringComparer.Equals(connectionString, other.connectionString) &&
                    parametersType == other.parametersType;
            }
        }

        public static int ExecuteQuery(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var paramObject = (object)param;
            IEnumerable multiExec = paramObject as IEnumerable;
            Identity identity;
            CacheInfo info = null;
            if (multiExec != null && !(multiExec is string))
            {
                bool isFirst = true;
                int total = 0;
                using (var cmd = SetupCommand(cnn, transaction, sql, null, null, commandTimeout, commandType))
                {
                    string masterSql = null;
                    foreach (var obj in multiExec)
                    {
                        if (isFirst)
                        {
                            masterSql = cmd.CommandText;
                            isFirst = false;
                            identity = new Identity(sql, cmd.CommandType, cnn, null, obj.GetType(), null);
                            info = GetCacheInfo(identity);
                        }
                        else
                        {
                            cmd.CommandText = masterSql;
                            cmd.Parameters.Clear();
                        }
                        info.ParamReader(cmd, obj);
                        total += cmd.ExecuteNonQuery();
                    }
                }
                return total;
            }


            if (paramObject != null)
            {
                identity = new Identity(sql, commandType, cnn, null, paramObject.GetType(), null);
                info = GetCacheInfo(identity);
            }
            return ExecuteCommandQuery(cnn, transaction, sql, paramObject == null ? null : info.ParamReader, paramObject, commandTimeout, commandType);
        }

        public static int ExecuteQueryMultiple(this IDbConnection cnn, IDbCommand cmd, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var paramObject = (object)param;
            IEnumerable multiExec = paramObject as IEnumerable;
            Identity identity;
            CacheInfo info = null;

            if (multiExec != null && !(multiExec is string))
            {
                bool isFirst = true;
                int total = 0;
                cmd = SetupCommand(cnn, cmd, transaction, sql, null, null, commandTimeout, commandType);
                {
                    string masterSql = null;
                    foreach (var obj in multiExec)
                    {
                        if (isFirst)
                        {
                            masterSql = cmd.CommandText;
                            isFirst = false;
                            identity = new Identity(sql, cmd.CommandType, cnn, null, obj.GetType(), null);
                            info = GetCacheInfo(identity);
                        }
                        else
                        {
                            cmd.CommandText = masterSql;
                            cmd.Parameters.Clear();
                        }
                        info.ParamReader(cmd, obj);
                        total += cmd.ExecuteNonQuery();
                    }
                }
                return total;
            }


            if (paramObject != null)
            {
                identity = new Identity(sql, commandType, cnn, null, paramObject.GetType(), null);
                info = GetCacheInfo(identity);
            }
            return ExecuteCommandQuery(cnn, transaction, sql, paramObject == null ? null : info.ParamReader, paramObject, commandTimeout, commandType);
        }

        public static object Execute(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var paramObject = (object)param;
            IEnumerable multiExec = paramObject as IEnumerable;
            Identity identity;
            CacheInfo info = null;
            if (multiExec != null && !(multiExec is string))
            {
                bool isFirst = true;
                int total = 0;
                using (var cmd = SetupCommand(cnn, transaction, sql, null, null, commandTimeout, commandType))
                {

                    string masterSql = null;
                    foreach (var obj in multiExec)
                    {
                        if (isFirst)
                        {
                            masterSql = cmd.CommandText;
                            isFirst = false;
                            identity = new Identity(sql, cmd.CommandType, cnn, null, obj.GetType(), null);
                            info = GetCacheInfo(identity);
                        }
                        else
                        {
                            cmd.CommandText = masterSql;
                            cmd.Parameters.Clear();
                        }
                        info.ParamReader(cmd, obj);
                        total += cmd.ExecuteNonQuery();
                    }
                }
                return total;
            }


            if (paramObject != null)
            {
                identity = new Identity(sql, commandType, cnn, null, paramObject.GetType(), null);
                info = GetCacheInfo(identity);
            }
            return ExecuteCommand(cnn, transaction, sql, paramObject == null ? null : info.ParamReader, paramObject, commandTimeout, commandType);
        }

        public static IEnumerable<dynamic> Query(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            return Query<DapperRow>(cnn, sql, (object)param, transaction, buffered, commandTimeout, commandType);
        }

        public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            var data = QueryInternal<T>(cnn, sql, (object)param, transaction, commandTimeout, commandType);
            return buffered ? data.ToList() : data;
        }

        public static GridReader QueryMultiple(this IDbConnection cnn, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var paramObject = (object)param;
            Identity identity = new Identity(sql, commandType, cnn, typeof(GridReader), paramObject == null ? null : paramObject.GetType(), null);
            CacheInfo info = GetCacheInfo(identity);

            IDbCommand cmd = null;
            IDataReader reader = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                {
                    cnn.Open();
                }

                cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, paramObject, commandTimeout, commandType);
                reader = cmd.ExecuteReader(wasClosed ? CommandBehavior.CloseConnection : CommandBehavior.Default);

                var result = new GridReader(cmd, reader, identity);
                wasClosed = false;



                return result;
            }
            catch
            {
                if (reader != null)
                {
                    if (!reader.IsClosed)
                    {
                        try { cmd.Cancel(); }
                        catch { }
                    }

                    reader.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }

                if (wasClosed)
                {
                    cnn.Close();
                }

                throw;
            }
        }

        private static IEnumerable<T> QueryInternal<T>(this IDbConnection cnn, string sql, object param, IDbTransaction transaction, int? commandTimeout, CommandType? commandType)
        {
            var identity = new Identity(sql, commandType, cnn, typeof(T), param == null ? null : param.GetType(), null);
            var info = GetCacheInfo(identity);

            IDbCommand cmd = null;
            IDataReader reader = null;

            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = SetupCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout, commandType);

                if (wasClosed)
                {
                    cnn.Open();
                }

                reader = cmd.ExecuteReader(wasClosed ? CommandBehavior.CloseConnection : CommandBehavior.Default);
                wasClosed = false;



                var tuple = info.Deserializer;
                int hash = GetColumnHash(reader);
                if (tuple.Func == null || tuple.Hash != hash)
                {
                    tuple = info.Deserializer = new DeserializerState(hash, GetDeserializer(typeof(T), reader, 0, -1, false));
                    SetQueryCache(identity, info);
                }

                var func = tuple.Func;

                while (reader.Read())
                {
                    yield return (T)func(reader);
                }


                reader.Dispose();
                reader = null;
            }
            finally
            {
                if (reader != null)
                {
                    if (!reader.IsClosed)
                    {
                        try { cmd.Cancel(); }
                        catch { }
                    }

                    reader.Dispose();
                }
                if (wasClosed)
                {
                    cnn.Close();
                }

                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return MultiMap<TFirst, TSecond, DontMap, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map, (object)param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(
    this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return MultiMap<TFirst, TSecond, TThird, DontMap, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map, (object)param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(
    this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, DontMap, DontMap, DontMap, TReturn>(cnn, sql, map, (object)param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(
    this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
    )
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, DontMap, DontMap, TReturn>(cnn, sql, map, (object)param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(
    this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null
    )
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, DontMap, TReturn>(cnn, sql, map, (object)param, transaction, buffered, splitOn, commandTimeout, commandType);
        }


        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(cnn, sql, map, (object)param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        partial class DontMap { }
        static IEnumerable<TReturn> MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
            this IDbConnection cnn, string sql, object map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType)
        {
            var results = MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(cnn, sql, map, param, transaction, splitOn, commandTimeout, commandType, null, null);
            return buffered ? results.ToList() : results;
        }


        static IEnumerable<TReturn> MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, string sql, object map, object param, IDbTransaction transaction, string splitOn, int? commandTimeout, CommandType? commandType, IDataReader reader, Identity identity)
        {
            identity = identity ?? new Identity(sql, commandType, cnn, typeof(TFirst), param == null ? null : param.GetType(), new[] { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh) });
            CacheInfo cinfo = GetCacheInfo(identity);

            IDbCommand ownedCommand = null;
            IDataReader ownedReader = null;

            bool wasClosed = cnn != null && cnn.State == ConnectionState.Closed;
            try
            {
                if (reader == null)
                {
                    ownedCommand = SetupCommand(cnn, transaction, sql, cinfo.ParamReader, param, commandTimeout, commandType);
                    if (wasClosed)
                    {
                        cnn.Open();
                    }

                    ownedReader = ownedCommand.ExecuteReader();
                    reader = ownedReader;
                }
                DeserializerState deserializer = default(DeserializerState);
                Func<IDataReader, object>[] otherDeserializers = null;

                int hash = GetColumnHash(reader);
                if ((deserializer = cinfo.Deserializer).Func == null || (otherDeserializers = cinfo.OtherDeserializers) == null || hash != deserializer.Hash)
                {
                    var deserializers = GenerateDeserializers(new[] { typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh) }, splitOn, reader);
                    deserializer = cinfo.Deserializer = new DeserializerState(hash, deserializers[0]);
                    otherDeserializers = cinfo.OtherDeserializers = deserializers.Skip(1).ToArray();
                    SetQueryCache(identity, cinfo);
                }

                Func<IDataReader, TReturn> mapIt = GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(deserializer.Func, otherDeserializers, map);

                if (mapIt != null)
                {
                    while (reader.Read())
                    {
                        yield return mapIt(reader);
                    }
                }
            }
            finally
            {
                try
                {
                    if (ownedReader != null)
                    {
                        ownedReader.Dispose();
                    }
                }
                finally
                {
                    if (ownedCommand != null)
                    {
                        ownedCommand.Dispose();
                    }
                    if (wasClosed)
                    {
                        cnn.Close();
                    }
                }
            }
        }

        private static Func<IDataReader, TReturn> GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Func<IDataReader, object> deserializer, Func<IDataReader, object>[] otherDeserializers, object map)
        {
            switch (otherDeserializers.Length)
            {
                case 1:
                    return r => ((Func<TFirst, TSecond, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r));
                case 2:
                    return r => ((Func<TFirst, TSecond, TThird, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r));
                case 3:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r));
                case 4:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r), (TFifth)otherDeserializers[3](r));
                case 5:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r), (TFifth)otherDeserializers[3](r), (TSixth)otherDeserializers[4](r));
                case 6:
                    return r => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>)map)((TFirst)deserializer(r), (TSecond)otherDeserializers[0](r), (TThird)otherDeserializers[1](r), (TFourth)otherDeserializers[2](r), (TFifth)otherDeserializers[3](r), (TSixth)otherDeserializers[4](r), (TSeventh)otherDeserializers[5](r));
                default:
                    throw new NotSupportedException();
            }
        }

        private static Func<IDataReader, object>[] GenerateDeserializers(Type[] types, string splitOn, IDataReader reader)
        {
            int current = 0;
            var splits = splitOn.Split(',').ToArray();
            var splitIndex = 0;

            Func<Type, int> nextSplit = type =>
            {
                var currentSplit = splits[splitIndex].Trim();
                if (splits.Length > splitIndex + 1)
                {
                    splitIndex++;
                }

                bool skipFirst = false;
                int startingPos = current + 1;

                if (type != typeof(object))
                {
                    var props = DefaultTypeMap.GetSettableProps(type);
                    var fields = DefaultTypeMap.GetSettableFields(type);

                    foreach (var name in props.Select(p => p.Name).Concat(fields.Select(f => f.Name)))
                    {
                        if (string.Equals(name, currentSplit, StringComparison.OrdinalIgnoreCase))
                        {
                            skipFirst = true;
                            startingPos = current;
                            break;
                        }
                    }

                }

                int pos;
                for (pos = startingPos; pos < reader.FieldCount; pos++)
                {

                    if (splitOn == "*")
                    {
                        break;
                    }
                    if (string.Equals(reader.GetName(pos), currentSplit, StringComparison.OrdinalIgnoreCase))
                    {
                        if (skipFirst)
                        {
                            skipFirst = false;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                current = pos;
                return pos;
            };

            var deserializers = new List<Func<IDataReader, object>>();
            int split = 0;
            bool first = true;
            foreach (var type in types)
            {
                if (type != typeof(DontMap))
                {
                    int next = nextSplit(type);
                    deserializers.Add(GetDeserializer(type, reader, split, next - split, !first));
                    first = false;
                    split = next;
                }
            }

            return deserializers.ToArray();
        }

        private static CacheInfo GetCacheInfo(Identity identity)
        {
            CacheInfo info;
            if (!TryGetQueryCache(identity, out info))
            {
                info = new CacheInfo();
                if (identity.parametersType != null)
                {
                    if (typeof(IDynamicParameters).IsAssignableFrom(identity.parametersType))
                    {
                        info.ParamReader = (cmd, obj) => { (obj as IDynamicParameters).AddParameters(cmd, identity); };
                    }
                    else if (typeof(IEnumerable<KeyValuePair<string, object>>).IsAssignableFrom(identity.parametersType) && typeof(System.Dynamic.IDynamicMetaObjectProvider).IsAssignableFrom(identity.parametersType))
                    {
                        info.ParamReader = (cmd, obj) => { IDynamicParameters mapped = new DynamicParameters(obj); mapped.AddParameters(cmd, identity); };
                    }
                    else
                    {
                        info.ParamReader = CreateParamInfoGenerator(identity, false);
                    }
                }
                SetQueryCache(identity, info);
            }
            return info;
        }

        private static Func<IDataReader, object> GetDeserializer(Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
        {

            if (type == typeof(object)
                || type == typeof(DapperRow))
            {
                return GetDapperRowDeserializer(reader, startBound, length, returnNullIfFirstMissing);
            }

            Type underlyingType = null;
            if (!(typeMap.ContainsKey(type) || type.IsEnum || type.FullName == LinqBinary ||
                (type.IsValueType && (underlyingType = Nullable.GetUnderlyingType(type)) != null && underlyingType.IsEnum)))
            {
                return GetTypeDeserializer(type, reader, startBound, length, returnNullIfFirstMissing);
            }
            return GetStructDeserializer(type, underlyingType ?? type, startBound);

        }

        private sealed partial class DapperTable
        {
            string[] fieldNames;
            readonly Dictionary<string, int> fieldNameLookup;

            internal string[] FieldNames { get { return fieldNames; } }

            public DapperTable(string[] fieldNames)
            {
                if (fieldNames == null)
                {
                    throw new ArgumentNullException(nameof(fieldNames));
                }

                this.fieldNames = fieldNames;

                fieldNameLookup = new Dictionary<string, int>(fieldNames.Length, StringComparer.Ordinal);

                for (int i = fieldNames.Length - 1; i >= 0; i--)
                {
                    string key = fieldNames[i];
                    if (key != null)
                    {
                        fieldNameLookup[key] = i;
                    }
                }
            }

            internal int IndexOfName(string name)
            {
                int result;
                return (name != null && fieldNameLookup.TryGetValue(name, out result)) ? result : -1;
            }
            internal int AddField(string name)
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                if (fieldNameLookup.ContainsKey(name))
                {
                    throw new InvalidOperationException("Field already exists: " + name);
                }

                int oldLen = fieldNames.Length;
                Array.Resize(ref fieldNames, oldLen + 1);
                fieldNames[oldLen] = name;
                fieldNameLookup[name] = oldLen;
                return oldLen;
            }


            internal bool FieldExists(string key)
            {
                return key != null && fieldNameLookup.ContainsKey(key);
            }

            public int FieldCount { get { return fieldNames.Length; } }
        }

        sealed partial class DapperRowMetaObject : System.Dynamic.DynamicMetaObject
        {
            static readonly MethodInfo getValueMethod = typeof(IDictionary<string, object>).GetProperty("Item").GetGetMethod();
            static readonly MethodInfo setValueMethod = typeof(DapperRow).GetMethod("SetValue", new[] { typeof(string), typeof(object) });

            public DapperRowMetaObject(
                System.Linq.Expressions.Expression expression,
                System.Dynamic.BindingRestrictions restrictions
                )
                : base(expression, restrictions)
            {
            }

            public DapperRowMetaObject(
                System.Linq.Expressions.Expression expression,
                System.Dynamic.BindingRestrictions restrictions,
                object value
                )
                : base(expression, restrictions, value)
            {
            }

            System.Dynamic.DynamicMetaObject CallMethod(
                MethodInfo method,
                System.Linq.Expressions.Expression[] parameters
                )
            {
                var callMethod = new System.Dynamic.DynamicMetaObject(
                    System.Linq.Expressions.Expression.Call(
                        System.Linq.Expressions.Expression.Convert(Expression, LimitType),
                        method,
                        parameters),
                    System.Dynamic.BindingRestrictions.GetTypeRestriction(Expression, LimitType)
                    );
                return callMethod;
            }

            public override System.Dynamic.DynamicMetaObject BindGetMember(System.Dynamic.GetMemberBinder binder)
            {
                var parameters = new System.Linq.Expressions.Expression[] { System.Linq.Expressions.Expression.Constant(binder.Name) };

                var callMethod = CallMethod(getValueMethod, parameters);

                return callMethod;
            }


            public override System.Dynamic.DynamicMetaObject BindInvokeMember(System.Dynamic.InvokeMemberBinder binder, System.Dynamic.DynamicMetaObject[] args)
            {
                var parameters = new System.Linq.Expressions.Expression[] { System.Linq.Expressions.Expression.Constant(binder.Name) };

                var callMethod = CallMethod(getValueMethod, parameters);

                return callMethod;
            }

            public override System.Dynamic.DynamicMetaObject BindSetMember(System.Dynamic.SetMemberBinder binder, System.Dynamic.DynamicMetaObject value)
            {
                var parameters = new System.Linq.Expressions.Expression[] { System.Linq.Expressions.Expression.Constant(binder.Name), value.Expression, };

                var callMethod = CallMethod(setValueMethod, parameters);

                return callMethod;
            }
        }

        private sealed partial class DapperRow
            : System.Dynamic.IDynamicMetaObjectProvider
            , IDictionary<string, object>
        {
            readonly DapperTable table;
            object[] values;

            public DapperRow(DapperTable table, object[] values)
            {
                if (table == null)
                {
                    throw new ArgumentNullException(nameof(table));
                }

                if (values == null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                this.table = table;
                this.values = values;
            }
            private sealed class DeadValue
            {
                public static readonly DeadValue Default = new DeadValue();
                private DeadValue() { }
            }
            int ICollection<KeyValuePair<string, object>>.Count
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (!(values[i] is DeadValue))
                        {
                            count++;
                        }
                    }
                    return count;
                }
            }

            public bool TryGetValue(string name, out object value)
            {
                var index = table.IndexOfName(name);
                if (index < 0)
                {
                    value = null;
                    return false;
                }

                value = index < values.Length ? values[index] : null;
                if (value is DeadValue)
                {
                    value = null;
                    return false;
                }
                return true;
            }

            public override string ToString()
            {
                var sb = new StringBuilder("{DapperRow");
                foreach (var kv in this)
                {
                    var value = kv.Value;
                    sb.Append(", ").Append(kv.Key);
                    if (value != null)
                    {
                        sb.Append(" = '").Append(kv.Value).Append('\'');
                    }
                    else
                    {
                        sb.Append(" = NULL");
                    }
                }

                return sb.Append('}').ToString();
            }

            System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(
                System.Linq.Expressions.Expression parameter)
            {
                return new DapperRowMetaObject(parameter, System.Dynamic.BindingRestrictions.Empty, this);
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                var names = table.FieldNames;
                for (var i = 0; i < names.Length; i++)
                {
                    object value = i < values.Length ? values[i] : null;
                    if (!(value is DeadValue))
                    {
                        yield return new KeyValuePair<string, object>(names[i], value);
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #region Implementation of ICollection<KeyValuePair<string,object>>

            void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
            {
                IDictionary<string, object> dic = this;
                dic.Add(item.Key, item.Value);
            }

            void ICollection<KeyValuePair<string, object>>.Clear()
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = DeadValue.Default;
                }
            }

            bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
            {
                object value;
                return TryGetValue(item.Key, out value) && Equals(value, item.Value);
            }

            void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                foreach (var kv in this)
                {
                    array[arrayIndex++] = kv;
                }
            }

            bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
            {
                IDictionary<string, object> dic = this;
                return dic.Remove(item.Key);
            }

            bool ICollection<KeyValuePair<string, object>>.IsReadOnly
            {
                get { return false; }
            }

            #endregion

            #region Implementation of IDictionary<string,object>

            bool IDictionary<string, object>.ContainsKey(string key)
            {
                int index = table.IndexOfName(key);
                if (index < 0 || index >= values.Length || values[index] is DeadValue)
                {
                    return false;
                }

                return true;
            }

            void IDictionary<string, object>.Add(string key, object value)
            {
                SetValue(key, value, true);
            }

            bool IDictionary<string, object>.Remove(string key)
            {
                int index = table.IndexOfName(key);
                if (index < 0 || index >= values.Length || values[index] is DeadValue)
                {
                    return false;
                }

                values[index] = DeadValue.Default;
                return true;
            }

            object IDictionary<string, object>.this[string key]
            {
                get { object val; TryGetValue(key, out val); return val; }
                set { SetValue(key, value, false); }
            }

            public object SetValue(string key, object value)
            {
                return SetValue(key, value, false);
            }
            private object SetValue(string key, object value, bool isAdd)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                int index = table.IndexOfName(key);
                if (index < 0)
                {
                    index = table.AddField(key);
                }
                else if (isAdd && index < values.Length && !(values[index] is DeadValue))
                {

                    throw new ArgumentException("An item with the same key has already been added", nameof(key));
                }
                int oldLength = values.Length;
                if (oldLength <= index)
                {


                    Array.Resize(ref values, table.FieldCount);
                    for (int i = oldLength; i < values.Length; i++)
                    {
                        values[i] = DeadValue.Default;
                    }
                }
                return values[index] = value;
            }

            ICollection<string> IDictionary<string, object>.Keys
            {
                get { return this.Select(kv => kv.Key).ToArray(); }
            }

            ICollection<object> IDictionary<string, object>.Values
            {
                get { return this.Select(kv => kv.Value).ToArray(); }
            }

            #endregion
        }

        internal static Func<IDataReader, object> GetDapperRowDeserializer(IDataRecord reader, int startBound, int length, bool returnNullIfFirstMissing)
        {
            var fieldCount = reader.FieldCount;
            if (length == -1)
            {
                length = fieldCount - startBound;
            }

            if (fieldCount <= startBound)
            {
                throw new ArgumentException("When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id", "splitOn");
            }

            var effectiveFieldCount = Math.Min(fieldCount - startBound, length);

            DapperTable table = null;

            return
                r =>
                {
                    if (table == null)
                    {
                        string[] names = new string[effectiveFieldCount];
                        for (int i = 0; i < effectiveFieldCount; i++)
                        {
                            names[i] = r.GetName(i + startBound);
                        }
                        table = new DapperTable(names);
                    }

                    var values = new object[effectiveFieldCount];

                    if (returnNullIfFirstMissing)
                    {
                        values[0] = r.GetValue(startBound);
                        if (values[0] is DBNull)
                        {
                            return null;
                        }
                    }

                    if (startBound == 0)
                    {
                        r.GetValues(values);
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] is DBNull)
                            {
                                values[i] = null;
                            }
                        }
                    }
                    else
                    {
                        var begin = returnNullIfFirstMissing ? 1 : 0;
                        for (var iter = begin; iter < effectiveFieldCount; ++iter)
                        {
                            object obj = r.GetValue(iter + startBound);
                            values[iter] = obj is DBNull ? null : obj;
                        }
                    }
                    return new DapperRow(table, values);
                };
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static char ReadChar(object value)
        {
            if (value == null || value is DBNull)
            {
                throw new ArgumentNullException(nameof(value));
            }

            string s = value as string;
            if (s == null || s.Length != 1)
            {
                throw new ArgumentException("A single-character was expected", nameof(value));
            }

            return s[0];
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static char? ReadNullableChar(object value)
        {
            if (value == null || value is DBNull)
            {
                return null;
            }

            string s = value as string;
            if (s == null || s.Length != 1)
            {
                throw new ArgumentException("A single-character was expected", nameof(value));
            }

            return s[0];
        }


        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static IDbDataParameter FindOrAddParameter(IDataParameterCollection parameters, IDbCommand command, string name)
        {
            IDbDataParameter result;
            if (parameters.Contains(name))
            {
                result = (IDbDataParameter)parameters[name];
            }
            else
            {
                result = command.CreateParameter();
                result.ParameterName = name;
                parameters.Add(result);
            }
            return result;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void PackListParameters(IDbCommand command, string namePrefix, object value)
        {



            var list = value as IEnumerable;
            var count = 0;

            if (list != null)
            {
                if (FeatureSupport.Get(command.Connection).Arrays)
                {
                    var arrayParm = command.CreateParameter();
                    arrayParm.Value = list;
                    arrayParm.ParameterName = namePrefix;
                    command.Parameters.Add(arrayParm);
                }
                else
                {
                    bool isString = value is IEnumerable<string>;
                    bool isDbString = value is IEnumerable<DbString>;
                    foreach (var item in list)
                    {
                        count++;
                        var listParam = command.CreateParameter();
                        listParam.ParameterName = namePrefix + count;
                        listParam.Value = item ?? DBNull.Value;
                        if (isString)
                        {
                            listParam.Size = 4000;
                            if (item != null && ((string)item).Length > 4000)
                            {
                                listParam.Size = -1;
                            }
                        }
                        if (isDbString && item as DbString != null)
                        {
                            var str = item as DbString;
                            str.AddParameter(command, listParam.ParameterName);
                        }
                        else
                        {
                            command.Parameters.Add(listParam);
                        }
                    }

                    if (count == 0)
                    {
                        command.CommandText = Regex.Replace(command.CommandText, @"[?@:]" + Regex.Escape(namePrefix), "(SELECT NULL WHERE 1 = 0)");
                    }
                    else
                    {
                        command.CommandText = Regex.Replace(command.CommandText, @"[?@:]" + Regex.Escape(namePrefix), match =>
                        {
                            var grp = match.Value;
                            var sb = new StringBuilder("(").Append(grp).Append(1);
                            for (int i = 2; i <= count; i++)
                            {
                                sb.Append(',').Append(grp).Append(i);
                            }
                            return sb.Append(')').ToString();
                        });
                    }
                }
            }

        }

        private static IEnumerable<PropertyInfo> FilterParameters(IEnumerable<PropertyInfo> parameters, string sql)
        {
            return parameters.Where(p => Regex.IsMatch(sql, "[@:]" + p.Name + "([^a-zA-Z0-9_]+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline));
        }

        public static Action<IDbCommand, object> CreateParamInfoGenerator(Identity identity, bool checkForDuplicates)
        {
            Type type = identity.parametersType;
            bool filterParams = identity.commandType.GetValueOrDefault(CommandType.Text) == CommandType.Text;
            var dm = new DynamicMethod(string.Format("ParamInfo{0}", Guid.NewGuid()), null, new[] { typeof(IDbCommand), typeof(object) }, type, true);

            var il = dm.GetILGenerator();

            il.DeclareLocal(type);
            bool haveInt32Arg1 = false;
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Unbox_Any, type);
            il.Emit(OpCodes.Stloc_0);

            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetProperty("Parameters").GetGetMethod(), null);

            IEnumerable<PropertyInfo> props = type.GetProperties().Where(p => p.GetIndexParameters().Length == 0).OrderBy(p => p.Name);
            if (filterParams)
            {
                props = FilterParameters(props, identity.sql);
            }
            foreach (var prop in props)
            {
                if (filterParams)
                {
                    if (identity.sql.IndexOf("@" + prop.Name, StringComparison.InvariantCultureIgnoreCase) < 0
                        && identity.sql.IndexOf(":" + prop.Name, StringComparison.InvariantCultureIgnoreCase) < 0)
                    {
                        continue;
                    }
                }
                if (typeof(ICustomQueryParameter).IsAssignableFrom(prop.PropertyType))
                {
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, prop.Name);
                    il.EmitCall(OpCodes.Callvirt, prop.PropertyType.GetMethod("AddParameter"), null);
                    continue;
                }
                DbType dbType = LookupDbType(prop.PropertyType, prop.Name);
                if (dbType == DynamicParameters.EnumerableMultiParameter)
                {

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, prop.Name);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                    if (prop.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, prop.PropertyType);
                    }
                    il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("PackListParameters"), null);
                    continue;
                }
                il.Emit(OpCodes.Dup);

                il.Emit(OpCodes.Ldarg_0);

                if (checkForDuplicates)
                {

                    il.Emit(OpCodes.Ldstr, prop.Name);
                    il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("FindOrAddParameter"), null);
                }
                else
                {

                    il.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetMethod("CreateParameter"), null);

                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldstr, prop.Name);
                    il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("ParameterName").GetSetMethod(), null);
                }
                if (dbType != DbType.Time)
                {
                    il.Emit(OpCodes.Dup);
                    EmitInt32(il, (int)dbType);

                    il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("DbType").GetSetMethod(), null);
                }

                il.Emit(OpCodes.Dup);
                EmitInt32(il, (int)ParameterDirection.Input);
                il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("Direction").GetSetMethod(), null);

                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                bool checkForNull = true;
                if (prop.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, prop.PropertyType);
                    if (Nullable.GetUnderlyingType(prop.PropertyType) == null)
                    {
                        checkForNull = false;
                    }
                }
                if (checkForNull)
                {
                    if (dbType == DbType.String && !haveInt32Arg1)
                    {
                        il.DeclareLocal(typeof(int));
                        haveInt32Arg1 = true;
                    }

                    il.Emit(OpCodes.Dup);
                    Label notNull = il.DefineLabel();
                    Label? allDone = dbType == DbType.String ? il.DefineLabel() : (Label? )null;
                    il.Emit(OpCodes.Brtrue_S, notNull);

                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField("Value"));
                    if (dbType == DbType.String)
                    {
                        EmitInt32(il, 0);
                        il.Emit(OpCodes.Stloc_1);
                    }
                    if (allDone != null)
                    {
                        il.Emit(OpCodes.Br_S, allDone.Value);
                    }

                    il.MarkLabel(notNull);
                    if (prop.PropertyType == typeof(string))
                    {
                        il.Emit(OpCodes.Dup);
                        il.EmitCall(OpCodes.Callvirt, typeof(string).GetProperty("Length").GetGetMethod(), null);
                        EmitInt32(il, 4000);
                        il.Emit(OpCodes.Cgt);
                        Label isLong = il.DefineLabel(), lenDone = il.DefineLabel();
                        il.Emit(OpCodes.Brtrue_S, isLong);
                        EmitInt32(il, 4000);
                        il.Emit(OpCodes.Br_S, lenDone);
                        il.MarkLabel(isLong);
                        EmitInt32(il, -1);
                        il.MarkLabel(lenDone);
                        il.Emit(OpCodes.Stloc_1);
                    }
                    if (prop.PropertyType.FullName == LinqBinary)
                    {
                        il.EmitCall(OpCodes.Callvirt, prop.PropertyType.GetMethod("ToArray", BindingFlags.Public | BindingFlags.Instance), null);
                    }
                    if (allDone != null)
                    {
                        il.MarkLabel(allDone.Value);
                    }

                }
                il.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("Value").GetSetMethod(), null);

                if (prop.PropertyType == typeof(string))
                {
                    var endOfSize = il.DefineLabel();

                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Brfalse_S, endOfSize);

                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldloc_1);
                    il.EmitCall(OpCodes.Callvirt, typeof(IDbDataParameter).GetProperty("Size").GetSetMethod(), null);

                    il.MarkLabel(endOfSize);
                }
                if (checkForDuplicates)
                {

                    il.Emit(OpCodes.Pop);
                }
                else
                {


                    il.EmitCall(OpCodes.Callvirt, typeof(IList).GetMethod("Add"), null);
                    il.Emit(OpCodes.Pop);
                }
            }

            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            return (Action<IDbCommand, object>)dm.CreateDelegate(typeof(Action<IDbCommand, object>));
        }

        private static IDbCommand SetupCommand(IDbConnection cnn, IDbTransaction transaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout, CommandType? commandType)
        {
            var cmd = cnn.CreateCommand();
            var bindByName = GetBindByName(cmd.GetType());
            if (bindByName != null)
            {
                bindByName(cmd, true);
            }

            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            cmd.CommandText = sql;
            if (commandTimeout.HasValue)
            {
                cmd.CommandTimeout = commandTimeout.Value;
            }

            if (commandType.HasValue)
            {
                cmd.CommandType = commandType.Value;
            }

            if (paramReader != null)
            {
                paramReader(cmd, obj);
            }
            return cmd;
        }

        private static IDbCommand SetupCommand(IDbConnection cnn, IDbCommand cmd, IDbTransaction transaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout, CommandType? commandType)
        {
            var bindByName = GetBindByName(cmd.GetType());
            if (bindByName != null)
            {
                bindByName(cmd, true);
            }

            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            cmd.CommandText = sql;
            if (commandTimeout.HasValue)
            {
                cmd.CommandTimeout = commandTimeout.Value;
            }

            if (commandType.HasValue)
            {
                cmd.CommandType = commandType.Value;
            }

            if (paramReader != null)
            {
                paramReader(cmd, obj);
            }
            return cmd;
        }

        private static object ExecuteCommand(IDbConnection cnn, IDbTransaction transaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout, CommandType? commandType)
        {
            IDbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = SetupCommand(cnn, transaction, sql, paramReader, obj, commandTimeout, commandType);
                if (wasClosed)
                {
                    cnn.Open();
                }

                return cmd.ExecuteScalar();
            }
            finally
            {
                if (wasClosed)
                {
                    cnn.Close();
                }

                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
        }

        private static int ExecuteCommandQuery(IDbConnection cnn, IDbTransaction transaction, string sql, Action<IDbCommand, object> paramReader, object obj, int? commandTimeout, CommandType? commandType)
        {
            IDbCommand cmd = null;
            bool wasClosed = cnn.State == ConnectionState.Closed;
            try
            {
                cmd = SetupCommand(cnn, transaction, sql, paramReader, obj, commandTimeout, commandType);
                if (wasClosed)
                {
                    cnn.Open();
                }

                var result = cmd.ExecuteNonQuery();
                return result;
            }
            finally
            {
                if (wasClosed)
                {
                    cnn.Close();
                }

                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
        }


        private static Func<IDataReader, object> GetStructDeserializer(Type type, Type effectiveType, int index)
        {

            if (type == typeof(char))
            {
                return r => SqlMapper.ReadChar(r.GetValue(index));
            }
            if (type == typeof(char?))
            {
                return r => SqlMapper.ReadNullableChar(r.GetValue(index));
            }
            if (type.FullName == LinqBinary)
            {
                return r => Activator.CreateInstance(type, r.GetValue(index));
            }

            if (effectiveType.IsEnum)
            {
                return r => { var val = r.GetValue(index); return val is DBNull ? null : Enum.ToObject(effectiveType, val); };
            }
            return r => { var val = r.GetValue(index); return val is DBNull ? null : val; };
        }

        static readonly MethodInfo
                    enumParse = typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) }),
                    getItem = typeof(IDataRecord).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => p.GetIndexParameters().Any() && p.GetIndexParameters()[0].ParameterType == typeof(int))
                        .Select(p => p.GetGetMethod()).First();

        public static ITypeMap GetTypeMap(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var map = (ITypeMap)_typeMaps[type];
            if (map == null)
            {
                lock (_typeMaps)
                {

                    map = (ITypeMap)_typeMaps[type];
                    if (map == null)
                    {
                        map = new DefaultTypeMap(type);
                        _typeMaps[type] = map;
                    }
                }
            }
            return map;
        }


        private static readonly Hashtable _typeMaps = new Hashtable();

        public static void SetTypeMap(Type type, ITypeMap map)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (map == null || map is DefaultTypeMap)
            {
                lock (_typeMaps)
                {
                    _typeMaps.Remove(type);
                }
            }
            else
            {
                lock (_typeMaps)
                {
                    _typeMaps[type] = map;
                }
            }

            PurgeQueryCacheByType(type);
        }

        public static Func<IDataReader, object> GetTypeDeserializer(
    Type type, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false)
        {

            var dm = new DynamicMethod(string.Format("Deserialize{0}", Guid.NewGuid()), typeof(object), new[] { typeof(IDataReader) }, true);
            var il = dm.GetILGenerator();
            il.DeclareLocal(typeof(int));
            il.DeclareLocal(type);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);

            if (length == -1)
            {
                length = reader.FieldCount - startBound;
            }

            if (reader.FieldCount <= startBound)
            {
                throw new ArgumentException("When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id", "splitOn");
            }

            var names = Enumerable.Range(startBound, length).Select(i => reader.GetName(i)).ToArray();

            ITypeMap typeMap = GetTypeMap(type);

            int index = startBound;

            ConstructorInfo specializedConstructor = null;

            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)1);
                il.Emit(OpCodes.Initobj, type);
            }
            else
            {
                var types = new Type[length];
                for (int i = startBound; i < startBound + length; i++)
                {
                    types[i - startBound] = reader.GetFieldType(i);
                }

                if (type.IsValueType)
                {
                    il.Emit(OpCodes.Ldloca_S, (byte)1);
                    il.Emit(OpCodes.Initobj, type);
                }
                else
                {
                    var ctor = typeMap.FindConstructor(names, types);
                    if (ctor == null)
                    {
                        string proposedTypes = "(" + String.Join(", ", types.Select((t, i) => t.FullName + " " + names[i]).ToArray()) + ")";
                        throw new InvalidOperationException(String.Format("A parameterless default constructor or one matching signature {0} is required for {1} materialization", proposedTypes, type.FullName));
                    }

                    if (ctor.GetParameters().Length == 0)
                    {
                        il.Emit(OpCodes.Newobj, ctor);
                        il.Emit(OpCodes.Stloc_1);
                    }
                    else
                    {
                        specializedConstructor = ctor;
                    }
                }
            }

            il.BeginExceptionBlock();
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)1);
            }
            else if (specializedConstructor == null)
            {
                il.Emit(OpCodes.Ldloc_1);
            }

            var members = (specializedConstructor != null ? names.Select(n => typeMap.GetConstructorParameter(specializedConstructor, n)) : names.Select(n => typeMap.GetMember(n))).ToList();



            bool first = true;
            var allDone = il.DefineLabel();
            int enumDeclareLocal = -1;
            foreach (var item in members)
            {
                if (item != null)
                {
                    if (specializedConstructor == null)
                    {
                        il.Emit(OpCodes.Dup);
                    }

                    Label isDbNullLabel = il.DefineLabel();
                    Label finishLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0);
                    EmitInt32(il, index);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc_0);
                    il.Emit(OpCodes.Callvirt, getItem);

                    Type memberType = item.MemberType;

                    if (memberType == typeof(char) || memberType == typeof(char?))
                    {
                        il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod(memberType == typeof(char) ? "ReadChar" : "ReadNullableChar", BindingFlags.Static | BindingFlags.Public), null);
                    }
                    else
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Isinst, typeof(DBNull));
                        il.Emit(OpCodes.Brtrue_S, isDbNullLabel);



                        var nullUnderlyingType = Nullable.GetUnderlyingType(memberType);
                        var unboxType = nullUnderlyingType != null && nullUnderlyingType.IsEnum ? nullUnderlyingType : memberType;

                        if (unboxType.IsEnum)
                        {
                            if (enumDeclareLocal == -1)
                            {
                                enumDeclareLocal = il.DeclareLocal(typeof(string)).LocalIndex;
                            }

                            Label isNotString = il.DefineLabel();
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Isinst, typeof(string));
                            il.Emit(OpCodes.Dup);
                            StoreLocal(il, enumDeclareLocal);
                            il.Emit(OpCodes.Brfalse_S, isNotString);

                            il.Emit(OpCodes.Pop);

                            il.Emit(OpCodes.Ldtoken, unboxType);
                            il.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);
                            il.Emit(OpCodes.Ldloc_2);
                            il.Emit(OpCodes.Ldc_I4_1);
                            il.EmitCall(OpCodes.Call, enumParse, null);

                            il.MarkLabel(isNotString);

                            il.Emit(OpCodes.Unbox_Any, unboxType);

                            if (nullUnderlyingType != null)
                            {
                                il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { nullUnderlyingType }));
                            }
                        }
                        else if (memberType.FullName == LinqBinary)
                        {
                            il.Emit(OpCodes.Unbox_Any, typeof(byte[]));
                            il.Emit(OpCodes.Newobj, memberType.GetConstructor(new[] { typeof(byte[]) }));
                        }
                        else
                        {
                            Type dataType = reader.GetFieldType(index);
                            TypeCode dataTypeCode = Type.GetTypeCode(dataType), unboxTypeCode = Type.GetTypeCode(unboxType);
                            if (dataType == unboxType || dataTypeCode == unboxTypeCode || dataTypeCode == Type.GetTypeCode(nullUnderlyingType))
                            {
                                il.Emit(OpCodes.Unbox_Any, unboxType);
                            }
                            else
                            {

                                bool handled = true;
                                OpCode opCode = default(OpCode);
                                if (dataTypeCode == TypeCode.Decimal || unboxTypeCode == TypeCode.Decimal)
                                {

                                    handled = false;
                                }
                                else
                                {
                                    switch (unboxTypeCode)
                                    {
                                        case TypeCode.Byte:
                                            opCode = OpCodes.Conv_Ovf_I1_Un; break;
                                        case TypeCode.SByte:
                                            opCode = OpCodes.Conv_Ovf_I1; break;
                                        case TypeCode.UInt16:
                                            opCode = OpCodes.Conv_Ovf_I2_Un; break;
                                        case TypeCode.Int16:
                                            opCode = OpCodes.Conv_Ovf_I2; break;
                                        case TypeCode.UInt32:
                                            opCode = OpCodes.Conv_Ovf_I4_Un; break;
                                        case TypeCode.Boolean:
                                        case TypeCode.Int32:
                                            opCode = OpCodes.Conv_Ovf_I4; break;
                                        case TypeCode.UInt64:
                                            opCode = OpCodes.Conv_Ovf_I8_Un; break;
                                        case TypeCode.Int64:
                                            opCode = OpCodes.Conv_Ovf_I8; break;
                                        case TypeCode.Single:
                                            opCode = OpCodes.Conv_R4; break;
                                        case TypeCode.Double:
                                            opCode = OpCodes.Conv_R8; break;
                                        default:
                                            handled = false;
                                            break;
                                    }
                                }
                                if (handled)
                                {
                                    il.Emit(OpCodes.Unbox_Any, dataType);
                                    il.Emit(opCode);
                                    if (unboxTypeCode == TypeCode.Boolean)
                                    {
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Ceq);
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Ceq);
                                    }
                                }
                                else
                                {
                                    il.Emit(OpCodes.Ldtoken, unboxType);
                                    il.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);
                                    il.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }), null);
                                    il.Emit(OpCodes.Unbox_Any, unboxType);
                                }

                            }

                        }
                    }
                    if (specializedConstructor == null)
                    {

                        if (item.Property != null)
                        {
                            if (type.IsValueType)
                            {
                                il.Emit(OpCodes.Call, DefaultTypeMap.GetPropertySetter(item.Property, type));
                            }
                            else
                            {
                                il.Emit(OpCodes.Callvirt, DefaultTypeMap.GetPropertySetter(item.Property, type));
                            }
                        }
                        else
                        {
                            il.Emit(OpCodes.Stfld, item.Field);
                        }
                    }

                    il.Emit(OpCodes.Br_S, finishLabel);

                    il.MarkLabel(isDbNullLabel);
                    if (specializedConstructor != null)
                    {
                        il.Emit(OpCodes.Pop);
                        if (item.MemberType.IsValueType)
                        {
                            int localIndex = il.DeclareLocal(item.MemberType).LocalIndex;
                            LoadLocalAddress(il, localIndex);
                            il.Emit(OpCodes.Initobj, item.MemberType);
                            LoadLocal(il, localIndex);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldnull);
                        }
                    }
                    else
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Pop);
                    }

                    if (first && returnNullIfFirstMissing)
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Stloc_1);
                        il.Emit(OpCodes.Br, allDone);
                    }

                    il.MarkLabel(finishLabel);
                }
                first = false;
                index += 1;
            }
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                if (specializedConstructor != null)
                {
                    il.Emit(OpCodes.Newobj, specializedConstructor);
                }
                il.Emit(OpCodes.Stloc_1);
            }
            il.MarkLabel(allDone);
            il.BeginCatchBlock(typeof(Exception));
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("ThrowDataException"), null);
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc_1);
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
            il.Emit(OpCodes.Ret);

            return (Func<IDataReader, object>)dm.CreateDelegate(typeof(Func<IDataReader, object>));
        }

        private static void LoadLocal(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue)
            {
                throw new ArgumentNullException(nameof(index));
            }

            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldloc_0); break;
                case 1: il.Emit(OpCodes.Ldloc_1); break;
                case 2: il.Emit(OpCodes.Ldloc_2); break;
                case 3: il.Emit(OpCodes.Ldloc_3); break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Ldloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, (short)index);
                    }
                    break;
            }
        }
        private static void StoreLocal(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue)
            {
                throw new ArgumentNullException(nameof(index));
            }

            switch (index)
            {
                case 0: il.Emit(OpCodes.Stloc_0); break;
                case 1: il.Emit(OpCodes.Stloc_1); break;
                case 2: il.Emit(OpCodes.Stloc_2); break;
                case 3: il.Emit(OpCodes.Stloc_3); break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Stloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc, (short)index);
                    }
                    break;
            }
        }
        private static void LoadLocalAddress(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (index <= 255)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, (short)index);
            }
        }
        public static void ThrowDataException(Exception ex, int index, IDataReader reader)
        {
            Exception toThrow;
            try
            {
                string name = "(n/a)", value = "(n/a)";
                if (reader != null && index >= 0 && index < reader.FieldCount)
                {
                    name = reader.GetName(index);
                    object val = reader.GetValue(index);
                    if (val == null || val is DBNull)
                    {
                        value = "<null>";
                    }
                    else
                    {
                        value = Convert.ToString(val) + " - " + Type.GetTypeCode(val.GetType());
                    }
                }
                toThrow = new DataException(string.Format("Error parsing column {0} ({1}={2})", index, name, value), ex);
            }
            catch
            {
                toThrow = new DataException(ex.Message, ex);
            }
            throw toThrow;
        }
        private static void EmitInt32(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }


        public static IEqualityComparer<string> ConnectionStringComparer
        {
            get { return connectionStringComparer; }
            set { connectionStringComparer = value ?? StringComparer.Ordinal; }
        }
        private static IEqualityComparer<string> connectionStringComparer = StringComparer.Ordinal;

        public partial class GridReader : IDisposable
        {
            private IDataReader reader;
            private IDbCommand command;
            private Identity identity;

            internal GridReader(IDbCommand command, IDataReader reader, Identity identity)
            {
                this.command = command;
                this.reader = reader;
                this.identity = identity;
            }

            public IEnumerable<dynamic> Read(bool buffered = true)
            {
                return Read<DapperRow>(buffered);
            }

            public IEnumerable<T> Read<T>(bool buffered = true)
            {
                if (reader == null)
                {
                    throw new ObjectDisposedException(GetType().FullName, "The reader has been disposed; this can happen after all data has been consumed");
                }

                if (consumed)
                {
                    throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                }

                var typedIdentity = identity.ForGrid(typeof(T), gridIndex);
                CacheInfo cache = GetCacheInfo(typedIdentity);
                var deserializer = cache.Deserializer;

                int hash = GetColumnHash(reader);
                if (deserializer.Func == null || deserializer.Hash != hash)
                {
                    deserializer = new DeserializerState(hash, GetDeserializer(typeof(T), reader, 0, -1, false));
                    cache.Deserializer = deserializer;
                }
                consumed = true;
                var result = ReadDeferred<T>(gridIndex, deserializer.Func, typedIdentity);
                return buffered ? result.ToList() : result;
            }

            private IEnumerable<TReturn> MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(object func, string splitOn)
            {
                var identity = this.identity.ForGrid(typeof(TReturn), new[] {
                    typeof(TFirst),
                    typeof(TSecond),
                    typeof(TThird),
                    typeof(TFourth),
                    typeof(TFifth),
                    typeof(TSixth),
                    typeof(TSeventh)
                }, gridIndex);
                try
                {
                    foreach (var r in SqlMapper.MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(null, null, func, null, null, splitOn, null, null, reader, identity))
                    {
                        yield return r;
                    }
                }
                finally
                {
                    NextResult();
                }
            }

            public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, DontMap, DontMap, DontMap, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, DontMap, DontMap, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, TFourth, DontMap, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, DontMap, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, DontMap, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }
            public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> func, string splitOn = "id", bool buffered = true)
            {
                var result = MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(func, splitOn);
                return buffered ? result.ToList() : result;
            }

            private IEnumerable<T> ReadDeferred<T>(int index, Func<IDataReader, object> deserializer, Identity typedIdentity)
            {
                try
                {
                    while (index == gridIndex && reader.Read())
                    {
                        yield return (T)deserializer(reader);
                    }
                }
                finally
                {
                    if (index == gridIndex)
                    {
                        NextResult();
                    }
                }
            }
            private int gridIndex, readCount;
            private bool consumed;
            private void NextResult()
            {
                if (reader.NextResult())
                {
                    readCount++;
                    gridIndex++;
                    consumed = false;
                }
                else
                {


                    reader.Dispose();
                    reader = null;

                    Dispose();
                }

            }
            public void Dispose()
            {
                if (reader != null)
                {
                    if (!reader.IsClosed && command != null)
                    {
                        command.Cancel();
                    }

                    reader.Dispose();
                    reader = null;
                }
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }
            }
        }
    }

}
