using MySqlConnector;
using Protocolo.Framework.Generic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Protocolo.Framework.Database
{
    public sealed class SqlManager
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(SqlManager));

        private MySqlDataSource m_dataSource;

        public MySqlConnection CreateConnection()
        {
            var dataSource = Volatile.Read(ref m_dataSource);
            if (dataSource == null)
                throw new InvalidOperationException("SqlManager must be initialized before creating connections.");

            return dataSource.OpenConnection();
        }

        public void Initialize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));

            var dataSource = new MySqlDataSource(connectionString);
            Interlocked.Exchange(ref m_dataSource, dataSource)?.Dispose();
        }

        public IEnumerable<T> Query<T>(string query, object param = null)
        {
            using (var connection = CreateConnection())
            {
                return connection.Query<T>(query, param);
            }
        }

        public T QuerySingle<T>(string query, object param = null)
        {
            using (var connection = CreateConnection())
            {
                return connection.Query<T>(query, param, buffered: false).FirstOrDefault();
            }
        }

        public int ExecuteQuery(string query, object param = null)
        {
            using (var connection = CreateConnection())
            {
                return connection.ExecuteQuery(query, param);
            }
        }

        public bool Insert<T>(T dataObject) where T : DataAccessObject<T>, new()
        {
            using (var connection = CreateConnection())
            {
                try
                {
                    connection.Insert<T>(dataObject);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error fatal al insertar en la base de datos: {ex.Message}");
                    return false;
                }
            }
        }

        public bool InsertWithKey<T>(T dataObject) where T : DataAccessObject<T>, new()
        {
            using (var connection = CreateConnection())
            {
                try
                {
                    connection.InsertWithKey<T>(dataObject);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error fatal al insertar en la base de datos: {ex.Message}");
                    return false;
                }
            }
        }

        public bool InsertWithKey<T>(IEnumerable<T> dataObjects) where T : DataAccessObject<T>, new()
        {
            return ExecuteTransaction(
                (connection, transaction) => connection.InsertWithKey(dataObjects, transaction),
                "insertar");
        }

        public bool Insert<T>(IEnumerable<T> dataObjects) where T : DataAccessObject<T>, new()
        {
            return ExecuteTransaction(
                (connection, transaction) => connection.Insert(dataObjects, transaction),
                "insertar");
        }

        public bool Delete<T>(T dataObject) where T : DataAccessObject<T>, new()
        {
            using (var connection = CreateConnection())
            {
                return connection.Delete<T>(dataObject);
            }
        }

        public void Delete<T>(MySqlConnection connection, MySqlTransaction transaction, IEnumerable<T> dataObjects) where T : DataAccessObject<T>, new()
        {
            connection.Delete<T>(dataObjects, transaction);
        }

        public void InsertWithKey<T>(MySqlConnection connection, MySqlTransaction transaction, IEnumerable<T> dataObjects) where T : DataAccessObject<T>, new()
        {
            connection.InsertWithKey<T>(dataObjects, transaction);
        }

        public void Update<T>(MySqlConnection connection, MySqlTransaction transaction, IEnumerable<T> dataObjects) where T : DataAccessObject<T>, new()
        {
            connection.Update<T>(dataObjects, transaction);
        }

        public bool Update<T>(T dataObject) where T : DataAccessObject<T>, new()
        {
            using (var connection = CreateConnection())
            {
                return connection.Update<T>(dataObject);
            }
        }

        private bool ExecuteTransaction(Action<MySqlConnection, MySqlTransaction> operation, string operationName)
        {
            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                operation(connection, transaction);
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rollbackException)
                {
                    Logger.Error($"Error al revertir la transacción de {operationName}: {rollbackException}");
                }

                Logger.Error($"Error fatal al {operationName} en la base de datos: {ex}");
                return false;
            }
        }
    }
}
