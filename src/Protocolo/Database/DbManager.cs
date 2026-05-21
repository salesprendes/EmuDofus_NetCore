using Protocolo.Framework.Generic;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocolo.Framework.Database
{
    public abstract class DbManager<T> : Singleton<T>
        where T : class, new()
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
                    Logger.Info(repository.GetType().Name + " : " + repository.ObjectCount + " datos cargados.");
                }
            }
            catch (MySqlException ex)
            {
                Logger.Error("Fatal error while loading database : connectionString=" + connectionString + " message=" + ex.ToString());
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
                    Logger.Error("DbManager::UpdateAll unable to update repositories : " + ex.Message);
                    try { transaction.Rollback(); } catch { }
                }
            }
        }
    }
}
