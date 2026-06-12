using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Protocolo.Framework.Database
{
    public static class SqlMapperExtensions
    {
        public interface IProxy
        {
            bool IsDirty { get; set; }
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, List<PropertyInfo>> KeyProperties = new ConcurrentDictionary<RuntimeTypeHandle, List<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, List<PropertyInfo>> TypeProperties = new ConcurrentDictionary<RuntimeTypeHandle, List<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> GetQueries = new ConcurrentDictionary<RuntimeTypeHandle, string>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TypeTableName = new ConcurrentDictionary<RuntimeTypeHandle, string>();
        private static readonly PropertyInfo[] NoKeyProperties = Array.Empty<PropertyInfo>();

        private static readonly Dictionary<string, ISqlAdapter> AdapterDictionary = new Dictionary<string, ISqlAdapter>
        {
            { "sqlconnection",    new SqlServerAdapter() },
            { "npgsqlconnection", new PostgresAdapter()  }
        };

        private static class Pluralizer
        {
            private static readonly Dictionary<string, string> Irregulars =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "person",     "people"    }, { "man",       "men"       },
                    { "child",      "children"  }, { "tooth",     "teeth"     },
                    { "foot",       "feet"      }, { "mouse",     "mice"      },
                    { "goose",      "geese"     }, { "ox",        "oxen"      },
                    { "criterion",  "criteria"  }, { "datum",     "data"      },
                    { "medium",     "media"     }, { "genus",     "genera"    },
                    { "index",      "indices"   }, { "matrix",    "matrices"  },
                    { "vertex",     "vertices"  }, { "axis",      "axes"      },
                    { "analysis",   "analyses"  }, { "crisis",    "crises"    },
                    { "basis",      "bases"     }, { "diagnosis", "diagnoses" },
                    { "thesis",     "theses"    }, { "phenomenon","phenomena" },
                };

            private static readonly HashSet<char> Vowels = new HashSet<char> { 'a', 'e', 'i', 'o', 'u' };

            public static string Pluralize(string word)
            {
                if (string.IsNullOrEmpty(word)) return word;

                if (Irregulars.TryGetValue(word, out var irregular))
                    return irregular;

                string lower = word.ToLower();


                if (lower.EndsWith("y") && word.Length > 1 && !Vowels.Contains(lower[word.Length - 2]))
                    return string.Concat(word.AsSpan(0, word.Length - 1), "ies");


                if (lower.EndsWith("fe"))
                    return string.Concat(word.AsSpan(0, word.Length - 2), "ves");


                if (lower.EndsWith("lf") || lower.EndsWith("rf") || lower.EndsWith("af"))
                    return string.Concat(word.AsSpan(0, word.Length - 1), "ves");


                if (lower.EndsWith("ss") || lower.EndsWith("sh") || lower.EndsWith("ch") || lower.EndsWith("x") || lower.EndsWith("z") || lower.EndsWith("s"))
                    return word + "es";


                if (lower.EndsWith("o") && word.Length > 1 && !Vowels.Contains(lower[word.Length - 2]))
                    return word + "es";

                return word + "s";
            }
        }

        private static List<PropertyInfo> KeyPropertiesCache(Type type)
        {
            if (KeyProperties.TryGetValue(type.TypeHandle, out var pi))
                return pi;

            var allProperties = TypePropertiesCache(type);
            var keyProperties = allProperties.Where(p => p.GetCustomAttributes(true).Any(a => a is KeyAttribute)).ToList();

            if (keyProperties.Count == 0)
            {
                var idProp = allProperties.FirstOrDefault(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase));
                if (idProp != null) keyProperties.Add(idProp);
            }

            KeyProperties[type.TypeHandle] = keyProperties;
            return keyProperties;
        }

        private static List<PropertyInfo> TypePropertiesCache(Type type)
        {
            if (TypeProperties.TryGetValue(type.TypeHandle, out var pis))
                return pis;


            var properties = type.GetProperties().Where(IsWriteable).ToList();
            TypeProperties[type.TypeHandle] = properties;
            return properties;
        }

        private static string QuoteIdentifier(string identifier) => "`" + identifier.Replace("`", "``") + "`";
        private static string BuildColumnList(IEnumerable<PropertyInfo> properties) => string.Join(", ", properties.Select(p => QuoteIdentifier(p.Name)));
        private static string BuildParameterList(IEnumerable<PropertyInfo> properties) => string.Join(", ", properties.Select(p => "@" + p.Name));

        private static string BuildUpdateSql(string tableName, IList<PropertyInfo> setCols, IList<PropertyInfo> whereCols)
        {
            var set = string.Join(", ", setCols.Select(p => $"{QuoteIdentifier(p.Name)} = @{p.Name}"));
            var where = string.Join(" and ", whereCols.Select(p => $"{QuoteIdentifier(p.Name)} = @{p.Name}"));
            return $"update {tableName} set {set} where {where}";
        }

        private static string BuildDeleteSql(string tableName, IList<PropertyInfo> keyProps)
        {
            var where = string.Join(" and ", keyProps.Select(p => $"{QuoteIdentifier(p.Name)} = @{p.Name}"));
            return $"delete from {tableName} where {where}";
        }

        public static bool IsWriteable(PropertyInfo pi)
        {
            var attributes = pi.GetCustomAttributes(typeof(WriteAttribute), false);
            if (attributes.Length == 1)
                return ((WriteAttribute)attributes[0]).Write;
            return true;
        }

        public static T Get<T>(this IDbConnection connection, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);

            if (!GetQueries.TryGetValue(type.TypeHandle, out string sql))
            {
                var keys = KeyPropertiesCache(type);
                if (keys.Count > 1)
                    throw new DataException("Get<T> only supports an entity with a single [Key] property");
                if (keys.Count == 0)
                    throw new DataException("Get<T> only supports an entity with a [Key] property");

                var onlyKey = keys[0];
                var name = GetTableName(type);

                var allProperties = TypePropertiesCache(type);
                var columnList = BuildColumnList(allProperties);

                sql = $"select {columnList} from {name} where {QuoteIdentifier(onlyKey.Name)} = @id";
                GetQueries[type.TypeHandle] = sql;
            }

            var dynParms = new DynamicParameters();
            dynParms.Add("@id", id);

            T obj = null;

            if (type.IsInterface)
            {
                var res = connection.Query(sql, dynParms, transaction: transaction, buffered: false, commandTimeout: commandTimeout).FirstOrDefault() as IDictionary<string, object>;
                if (res == null)
                    return null;

                obj = ProxyGenerator.GetInterfaceProxy<T>();
                foreach (var property in TypePropertiesCache(type))
                {
                    if (res.TryGetValue(property.Name, out var val))
                        property.SetValue(obj, val, null);
                }
                ((IProxy)obj).IsDirty = false;
            }
            else
            {
                obj = connection.Query<T>(sql, dynParms, transaction: transaction, buffered: false, commandTimeout: commandTimeout).FirstOrDefault();
            }

            return obj;
        }


        public static void SetTableName(Type type, string name) => TypeTableName[type.TypeHandle] = name;

        public static string GetTableName(Type type)
        {
            if (!TypeTableName.TryGetValue(type.TypeHandle, out string name))
            {

                string baseName = type.Name;
                if (type.IsInterface && baseName.StartsWith("I") && baseName.Length > 1)
                    baseName = baseName.AsSpan(1).ToString();

                name = Pluralizer.Pluralize(baseName);

                var tableattr = type.GetCustomAttributes(false).Where(attr => attr.GetType().Name == "TableAttribute").SingleOrDefault() as dynamic;

                if (tableattr != null)
                    name = tableattr.Name;

                TypeTableName[type.TypeHandle] = name;
            }
            return name;
        }





        public static void InsertWithKey<T>(this IDbConnection connection, IEnumerable<T> entities,
                                            IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var name = GetTableName(type);
            var allProps = TypePropertiesCache(type);
            var columns = BuildColumnList(allProps);
            var parameters = BuildParameterList(allProps);
            var adapter = GetFormatter(connection);
            foreach (var entity in entities)
                adapter.Insert(connection, transaction, commandTimeout, name, columns, parameters, NoKeyProperties, entity);
        }

        public static long InsertWithKey<T>(this IDbConnection connection, T entityToInsert,
                                            IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var name = GetTableName(type);
            var allProps = TypePropertiesCache(type);
            var adapter = GetFormatter(connection);

            return adapter.Insert(connection, transaction, commandTimeout, name, BuildColumnList(allProps), BuildParameterList(allProps), NoKeyProperties, entityToInsert);
        }

        public static long Insert<T>(this IDbConnection connection, T entityToInsert,
                                     IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var name = GetTableName(type);
            var keyProps = KeyPropertiesCache(type);
            var nonKeyProps = TypePropertiesCache(type).Except(keyProps).ToList();
            var adapter = GetFormatter(connection);

            return adapter.Insert(connection, transaction, commandTimeout, name, BuildColumnList(nonKeyProps), BuildParameterList(nonKeyProps), keyProps, entityToInsert);
        }

        public static void Insert<T>(this IDbConnection connection, IEnumerable<T> entities,
                                     IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var name = GetTableName(type);
            var keyProps = KeyPropertiesCache(type);
            var nonKeyProps = TypePropertiesCache(type).Except(keyProps).ToList();
            var columns = BuildColumnList(nonKeyProps);
            var parameters = BuildParameterList(nonKeyProps);
            var adapter = GetFormatter(connection);

            foreach (var entity in entities)
                adapter.Insert(connection, transaction, commandTimeout, name, columns, parameters, keyProps, entity);
        }





        public static int Update<T>(this IDbConnection connection, IEnumerable<T> entitiesToUpdate,
                                    IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var keyProps = KeyPropertiesCache(type);
            if (!keyProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var nonIdProps = TypePropertiesCache(type).Except(keyProps).ToList();
            if (!nonIdProps.Any()) return 0;

            var sql = BuildUpdateSql(GetTableName(type), nonIdProps, keyProps);
            return connection.ExecuteQuery(sql, entitiesToUpdate, commandTimeout: commandTimeout, transaction: transaction);
        }

        public static int UpdateTransactional<T>(this IDbConnection connection, IDbCommand cmd,
                                          IEnumerable<T> entitiesToUpdate,
                                          IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var keyProps = KeyPropertiesCache(type);
            if (!keyProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var nonIdProps = TypePropertiesCache(type).Except(keyProps).ToList();
            var sql = BuildUpdateSql(GetTableName(type), nonIdProps, keyProps);

            return connection.ExecuteQueryMultiple(cmd, sql, entitiesToUpdate, commandTimeout: commandTimeout, transaction: transaction);
        }

        public static bool Update<T>(this IDbConnection connection, T entityToUpdate,
                             IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var proxy = entityToUpdate as IProxy;
            if (proxy != null && !proxy.IsDirty)
                return false;

            var type = typeof(T);
            var keyProps = KeyPropertiesCache(type);
            if (!keyProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var nonIdProps = TypePropertiesCache(type).Except(keyProps).ToList();
            var sql = BuildUpdateSql(GetTableName(type), nonIdProps, keyProps);

            return connection.ExecuteQuery(sql, entityToUpdate, commandTimeout: commandTimeout, transaction: transaction) > 0;
        }





        public static bool Delete<T>(this IDbConnection connection, T entityToDelete,
                             IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            if (entityToDelete == null)
                throw new ArgumentException("Cannot Delete null Object", nameof(entityToDelete));

            var type = typeof(T);
            var keyProps = KeyPropertiesCache(type);
            if (!keyProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var sql = BuildDeleteSql(GetTableName(type), keyProps);
            return connection.ExecuteQuery(sql, entityToDelete, transaction: transaction, commandTimeout: commandTimeout) > 0;
        }

        public static void Delete<T>(this IDbConnection connection, IEnumerable<T> entities,
                             IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var list = entities as IList<T> ?? entities.ToList();
            if (!list.Any()) return;

            var type = typeof(T);
            var keyProps = KeyPropertiesCache(type);
            if (!keyProps.Any())
                throw new ArgumentException("Entity must have at least one [Key] property");

            var sql = BuildDeleteSql(GetTableName(type), keyProps);
            foreach (var entity in list)
                connection.ExecuteQuery(sql, entity, transaction: transaction, commandTimeout: commandTimeout);
        }





        public static ISqlAdapter GetFormatter(IDbConnection connection)
        {
            string name = connection.GetType().Name.ToLower();
            return AdapterDictionary.TryGetValue(name, out var adapter) ? adapter : new SqlServerAdapter();
        }





        public static class ProxyGenerator
        {



            private static readonly ConcurrentDictionary<Type, Type> TypeCache = new ConcurrentDictionary<Type, Type>();

            private static AssemblyBuilder GetAsmBuilder(string name)
                => AssemblyBuilder.DefineDynamicAssembly(
                       new AssemblyName { Name = name }, AssemblyBuilderAccess.Run);

            public static T GetClassProxy<T>()
            {



                throw new NotImplementedException();
            }

            public static T GetInterfaceProxy<T>()
            {
                Type typeOfT = typeof(T);



                var generatedType = TypeCache.GetOrAdd(typeOfT, t =>
                {
                    var assemblyBuilder = GetAsmBuilder(t.Name);
                    var moduleBuilder = assemblyBuilder.DefineDynamicModule("SqlMapperExtensions." + t.Name);

                    var typeBuilder = moduleBuilder.DefineType(t.Name + "_" + Guid.NewGuid(), TypeAttributes.Public | TypeAttributes.Class);

                    typeBuilder.AddInterfaceImplementation(t);
                    typeBuilder.AddInterfaceImplementation(typeof(SqlMapperExtensions.IProxy));

                    var setIsDirtyMethod = CreateIsDirtyProperty(typeBuilder);

                    foreach (var property in t.GetProperties())
                    {
                        bool isId = property.GetCustomAttributes(true).Any(a => a is KeyAttribute);
                        CreateProperty<T>(typeBuilder, property, setIsDirtyMethod, isId);
                    }

                    return typeBuilder.CreateType();
                });



                return (T)Activator.CreateInstance(generatedType);
            }

            private static MethodInfo CreateIsDirtyProperty(TypeBuilder typeBuilder)
            {
                var propType = typeof(bool);
                var field = typeBuilder.DefineField("_IsDirty", propType, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty("IsDirty", System.Reflection.PropertyAttributes.None, propType, new[] { propType });

                const MethodAttributes getSetAttr =
                    MethodAttributes.Public | MethodAttributes.NewSlot |
                    MethodAttributes.SpecialName | MethodAttributes.Final |
                    MethodAttributes.Virtual | MethodAttributes.HideBySig;


                var getter = typeBuilder.DefineMethod("get_IsDirty", getSetAttr, propType, Type.EmptyTypes);
                var getterIL = getter.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, field);
                getterIL.Emit(OpCodes.Ret);


                var setter = typeBuilder.DefineMethod("set_IsDirty", getSetAttr, null, new[] { propType });
                var setterIL = setter.GetILGenerator();
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, field);
                setterIL.Emit(OpCodes.Ret);

                property.SetGetMethod(getter);
                property.SetSetMethod(setter);
                typeBuilder.DefineMethodOverride(getter, typeof(IProxy).GetMethod("get_IsDirty"));
                typeBuilder.DefineMethodOverride(setter, typeof(IProxy).GetMethod("set_IsDirty"));

                return setter;
            }

            private static void CreateProperty<T>(TypeBuilder typeBuilder,
                                                   PropertyInfo interfaceProperty,
                                                   MethodInfo setIsDirtyMethod,
                                                   bool isIdentity)
            {
                string propertyName = interfaceProperty.Name;
                Type propType = interfaceProperty.PropertyType;

                var field = typeBuilder.DefineField("_" + propertyName, propType, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty(propertyName, System.Reflection.PropertyAttributes.None, propType, new[] { propType });

                const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;


                var getter = typeBuilder.DefineMethod("get_" + propertyName, getSetAttr, propType, Type.EmptyTypes);
                var getterIL = getter.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, field);
                getterIL.Emit(OpCodes.Ret);


                var setter = typeBuilder.DefineMethod("set_" + propertyName, getSetAttr, null, new[] { propType });
                var setterIL = setter.GetILGenerator();
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, field);
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldc_I4_1);
                setterIL.Emit(OpCodes.Call, setIsDirtyMethod);
                setterIL.Emit(OpCodes.Ret);

                foreach (var attrData in interfaceProperty.GetCustomAttributesData())
                {
                    try
                    {
                        var ctorArgs = attrData.ConstructorArguments.Select(a => a.Value).ToArray();
                        var namedFields = attrData.NamedArguments.Where(a => a.IsField).Select(a => (FieldInfo)a.MemberInfo).ToArray();
                        var namedFieldVals = attrData.NamedArguments.Where(a => a.IsField).Select(a => a.TypedValue.Value).ToArray();
                        var namedProps = attrData.NamedArguments.Where(a => !a.IsField).Select(a => (PropertyInfo)a.MemberInfo).ToArray();
                        var namedPropVals = attrData.NamedArguments.Where(a => !a.IsField).Select(a => a.TypedValue.Value).ToArray();

                        property.SetCustomAttribute(new CustomAttributeBuilder(attrData.Constructor, ctorArgs, namedProps, namedPropVals, namedFields, namedFieldVals));
                    }
                    catch
                    {


                    }
                }

                property.SetGetMethod(getter);
                property.SetSetMethod(setter);
                typeBuilder.DefineMethodOverride(getter, typeof(T).GetMethod("get_" + propertyName));
                typeBuilder.DefineMethodOverride(setter, typeof(T).GetMethod("set_" + propertyName));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string tableName) { Name = tableName; }
        public string Name { get; private set; }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class WriteAttribute : Attribute
    {
        public WriteAttribute(bool write) { Write = write; }
        public bool Write { get; private set; }
    }

    public interface ISqlAdapter
    {
        int Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
                   string tableName, string columnList, string parameterList,
                   IEnumerable<PropertyInfo> keyProperties, object entityToInsert);
    }

    public class SqlServerAdapter : ISqlAdapter
    {
        public int Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
                          string tableName, string columnList, string parameterList,
                          IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
        {
            string cmd = $"insert into {tableName} ({columnList}) values ({parameterList})";
            connection.Execute(cmd, entityToInsert, transaction: transaction, commandTimeout: commandTimeout);
            return 1;
        }
    }

    public class PostgresAdapter : ISqlAdapter
    {
        public int Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
                          string tableName, string columnList, string parameterList,
                          IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("insert into {0} ({1}) values ({2})", tableName, columnList, parameterList);

            var keyList = keyProperties.ToList();


            if (!keyList.Any())
            {
                connection.Execute(sb.ToString(), entityToInsert, transaction: transaction, commandTimeout: commandTimeout);
                return 0;
            }

            sb.Append(" RETURNING ");
            sb.Append(string.Join(", ", keyList.Select(p => p.Name)));

            var row = connection.Query(sb.ToString(), entityToInsert,
                                       transaction: transaction, buffered: false, commandTimeout: commandTimeout)
                                .FirstOrDefault() as IDictionary<string, object>;
            if (row == null)
            {
                return 0;
            }


            int id = 0;
            foreach (var p in keyList)
            {
                var value = row[p.Name.ToLower()];
                p.SetValue(entityToInsert, value, null);
                if (id == 0)
                    id = Convert.ToInt32(value);
            }
            return id;
        }
    }
}
