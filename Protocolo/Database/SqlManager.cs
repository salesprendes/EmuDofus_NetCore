using MySqlConnector;
using Protocolo.Framework.Generic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Protocolo.Framework.Database
{
    public sealed class SqlManager
    {
        public static ILogger Logger = LogManager.GetLogger(typeof(SqlManager));

        private string m_connectionString;

        public MySqlConnection CreateConnection()
        {
            var connection = new MySqlConnection(m_connectionString);
            connection.Open();
            return connection;
        }

        public void Initialize(string connectionString)
        {
            m_connectionString = connectionString;
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
                    Logger.Error("Error fatal al insertar en la base de datos: " + ex.Message);
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
                    Logger.Error("Error fatal al insertar en la base de datos: " + ex.Message);
                    return false;
                }
            }
        }

        public bool InsertWithKey<T>(IEnumerable<T> dataObjects) where T : DataAccessObject<T>, new()
        {
            using (var connection = CreateConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        connection.InsertWithKey<T>(dataObjects, transaction);
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error("Error fatal al insertar en la base de datos: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public bool Insert<T>(IEnumerable<T> dataObjects) where T : DataAccessObject<T>, new()
        {
            using (var connection = CreateConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        connection.Insert<T>(dataObjects, transaction);
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error("Error fatal al insertar en la base de datos: " + ex.Message);
                        return false;
                    }
                }
            }
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
    }
}
