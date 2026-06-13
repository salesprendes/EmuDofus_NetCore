using MySqlConnector;
using Protocolo.Framework.Generic;
using System;
using System.Collections.Generic;

namespace Protocolo.Framework.Database
{
    public abstract class DbManager<T> : Singleton<T> where T : class, new()
    {
        private readonly List<IRepository> m_repositories;
        private readonly SqlManager m_sqlMgr;

        public DbManager()
        {
            m_repositories = new List<IRepository>();
            m_sqlMgr = new SqlManager();
        }

        public virtual void LoadAll(string connectionString)
        {
            m_sqlMgr.Initialize(connectionString);

            try
            {
                foreach (var repository in m_repositories)
                {
                    repository.Initialize(m_sqlMgr);
                    Logger.Info($"{repository.GetType().Name} : {repository.ObjectCount} datos cargados.");
                }
            }
            catch (MySqlException ex)
            {
                Logger.Error($"Error fatal al cargar la base de datos: cadenaConexion={connectionString} mensaje={ex}");
            }
        }

        public void AddRepository(IRepository repository)
        {
            m_repositories.Add(repository);
        }

        public void UpdateAll()
        {
            using (var connection = m_sqlMgr.CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (var repo in m_repositories)
                    {
                        repo.DeleteAll(connection, transaction);
                        repo.InsertAll(connection, transaction);
                        repo.UpdateAll(connection, transaction);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Logger.Error($"DbManager::UpdateAll no se pudieron actualizar los repositorios: {ex.Message}");
                    try { transaction.Rollback(); } catch { }
                }
            }
        }
    }
}
