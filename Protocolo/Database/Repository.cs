using MySqlConnector;
using Protocolo.Framework.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Protocolo.Framework.Database
{
    public interface IRepository
    {
        void Initialize(SqlManager sqlManager);

        int ObjectCount
        {
            get;
        }

        void UpdateAll(MySqlConnection connection, MySqlTransaction transaction);

        void DeleteAll(MySqlConnection connection, MySqlTransaction transaction);

        void InsertAll(MySqlConnection connection, MySqlTransaction transaction);
    }

    public abstract class Repository<TRepository, TDataObject> : Singleton<TRepository>, IRepository
    where TDataObject : DataAccessObject<TDataObject>, new()
    where TRepository : class, new()
    {
        private static readonly string TableName = SqlMapperExtensions.GetTableName(typeof(TDataObject));

        public SqlManager SqlMgr
        {
            get;
            private set;
        }

        protected readonly Lock m_syncLock = new Lock();

        protected readonly List<TDataObject> m_dataObjects;

        private readonly List<TDataObject> m_updateBuffer;
        private readonly List<TDataObject> m_insertBuffer;
        private readonly List<TDataObject> m_deleteBuffer;

        public int ObjectCount
        {
            get
            {
                lock (m_syncLock)
                    return m_dataObjects.Count;
            }
        }


        public IEnumerable<TDataObject> All
        {
            get
            {
                lock (m_syncLock)
                    return m_dataObjects.ToArray();
            }
        }

        public List<TDataObject> UpdateObjects
        {
            get
            {
                lock (m_syncLock)
                {
                    m_updateBuffer.Clear();
                    foreach (var obj in m_dataObjects)
                    {
                        if (obj.IsDirty && !obj.IsNew && !obj.IsDeleted)
                        {
                            obj.OnBeforeUpdate();
                            obj.IsDirty = false;
                            m_updateBuffer.Add(obj);
                        }
                    }
                }
                return m_updateBuffer;
            }
        }

        public List<TDataObject> InsertObjects
        {
            get
            {
                lock (m_syncLock)
                {
                    m_insertBuffer.Clear();
                    foreach (var obj in m_dataObjects)
                    {
                        if (obj.IsNew && !obj.IsDeleted)
                        {
                            obj.OnBeforeInsert();
                            obj.IsNew = false;
                            obj.IsDirty = false;
                            m_insertBuffer.Add(obj);
                        }
                    }
                }
                return m_insertBuffer;
            }
        }

        public List<TDataObject> DeleteObjects
        {
            get
            {
                lock (m_syncLock)
                {
                    m_deleteBuffer.Clear();
                    foreach (var obj in m_dataObjects)
                    {
                        if (obj.IsDeleted && !obj.IsNew)
                        {
                            obj.OnBeforeDelete();
                            obj.IsDeleted = false;
                            obj.IsDirty = false;
                            m_deleteBuffer.Add(obj);
                        }
                    }
                }
                return m_deleteBuffer;
            }
        }

        public IEnumerable<TDataObject> SelectAll
        {
            get
            {
                return SqlMgr.Query<TDataObject>("select * from " + TableName);
            }
        }

        public bool LoadOnly
        {
            get;
            private set;
        }

        public bool ReadOnly
        {
            get;
            private set;
        }

        public Repository(bool loadOnly = false, bool readOnly = false)
        {
            m_dataObjects = new List<TDataObject>();
            m_updateBuffer = new List<TDataObject>();
            m_insertBuffer = new List<TDataObject>();
            m_deleteBuffer = new List<TDataObject>();
            LoadOnly = loadOnly;
            ReadOnly = readOnly;
        }

        private void OnObjectPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var obj = (TDataObject)sender;
            if (DataAccessObject<TDataObject>.IsRunning && !obj.IsNew && !obj.IsDeleted)
                obj.IsDirty = true;
        }

        private void Subscribe(TDataObject obj) => obj.PropertyChanged += OnObjectPropertyChanged;
        private void Unsubscribe(TDataObject obj) => obj.PropertyChanged -= OnObjectPropertyChanged;

        public virtual void Initialize(SqlManager sqlMgr)
        {
            SqlMgr = sqlMgr;

            if (!LoadOnly)
            {
                IEnumerable<TDataObject> objects = SqlMgr.Query<TDataObject>("select * from " + TableName);
                m_dataObjects.AddRange(objects);
                foreach (var obj in m_dataObjects)
                {
                    if (!ReadOnly)
                        Subscribe(obj);
                    OnObjectAdded(obj);
                }
                if (!ReadOnly)
                {
                    DataAccessObject<TDataObject>.IsRunning = true;
                    DataAccessObject<TDataObject>.SqlMgr = SqlMgr;
                }
            }
        }

        public virtual TDataObject Load(string query, dynamic param = null)
        {
            return LoadMultiple(query, (object)param).FirstOrDefault();
        }

        public virtual IEnumerable<TDataObject> LoadMultiple(string query, dynamic param = null)
        {
            IEnumerable<TDataObject> objects = SqlMgr.Query<TDataObject>("select * from " + TableName + " where " + query, (object)param);

            if (!LoadOnly)
            {
                lock (m_syncLock)
                {
                    foreach (var obj in objects)
                    {
                        m_dataObjects.Add(obj);
                        if (!ReadOnly)
                            Subscribe(obj);
                        OnObjectAdded(obj);
                    }
                }
            }

            return objects;
        }

        public virtual void Update(MySqlConnection connection, MySqlTransaction transaction, List<TDataObject> objects)
        {
            if (objects.Count > 0)
                SqlMgr.Update<TDataObject>(connection, transaction, objects);
        }

        public virtual void Delete(MySqlConnection connection, MySqlTransaction transaction, List<TDataObject> objects)
        {
            if (objects.Count > 0)
                SqlMgr.Delete<TDataObject>(connection, transaction, objects);
        }

        public virtual void Insert(MySqlConnection connection, MySqlTransaction transaction, List<TDataObject> objects)
        {
            if (objects.Count > 0)
                SqlMgr.InsertWithKey<TDataObject>(connection, transaction, objects);
        }

        public virtual bool Delete(TDataObject obj)
        {
            if (ReadOnly)
                return false;

            var result = SqlMgr.Delete<TDataObject>(obj);
            if (!LoadOnly)
            {
                if (result)
                {
                    lock (m_syncLock)
                    {
                        Unsubscribe(obj);
                        m_dataObjects.Remove(obj);
                        OnObjectRemoved(obj);
                    }
                }
            }
            return result;
        }

        public virtual void Removed(IEnumerable<TDataObject> objects)
        {
            if (ReadOnly)
                return;

            lock (m_syncLock)
                foreach (TDataObject obj in objects)
                    RemovedLocked(obj);
        }

        public virtual void Removed(TDataObject obj)
        {
            if (ReadOnly)
                return;

            lock (m_syncLock)
                RemovedLocked(obj);
        }

        private void RemovedLocked(TDataObject obj)
        {
            Unsubscribe(obj);
            
            if (obj.IsNew)
                m_dataObjects.Remove(obj);

            OnObjectRemoved(obj);
            obj.IsDeleted = true;
        }

        public virtual void Created(TDataObject obj)
        {
            if (ReadOnly)
                return;

            lock (m_syncLock)
            {
                m_dataObjects.Add(obj);
                Subscribe(obj);
                OnObjectAdded(obj);
                obj.IsNew = true;
            }
        }

        public virtual bool Insert(TDataObject obj)
        {
            if (ReadOnly)
                return false;

            var result = SqlMgr.InsertWithKey<TDataObject>(obj);
            if (!LoadOnly)
            {
                if (result)
                {
                    lock (m_syncLock)
                    {
                        m_dataObjects.Add(obj);
                        Subscribe(obj);
                        OnObjectAdded(obj);
                    }
                }
            }
            return result;
        }

        public TDataObject Find(Predicate<TDataObject> match)
        {
            lock (m_syncLock)
                return m_dataObjects.Find(match);
        }

        public IEnumerable<TDataObject> FindAll(Predicate<TDataObject> match)
        {
            lock (m_syncLock)
                return m_dataObjects.FindAll(match);
        }

        public virtual void UpdateAll(MySqlConnection connection, MySqlTransaction transaction)
        {
            if (ReadOnly)
                return;

            Update(connection, transaction, UpdateObjects);
        }

        public virtual void DeleteAll(MySqlConnection connection, MySqlTransaction transaction)
        {
            if (ReadOnly)
                return;

            var toDelete = DeleteObjects;
            Delete(connection, transaction, toDelete);
            if (toDelete.Count > 0)
            {
                lock (m_syncLock)
                    foreach (var obj in toDelete)
                    {
                        Unsubscribe(obj);
                        m_dataObjects.Remove(obj);
                    }
            }
        }

        public virtual void InsertAll(MySqlConnection connection, MySqlTransaction transaction)
        {
            if (ReadOnly)
                return;

            Insert(connection, transaction, InsertObjects);
        }

        public virtual void OnObjectAdded(TDataObject obj)
        {
        }

        public virtual void OnObjectRemoved(TDataObject obj)
        {
        }

        public void ImplicitDeletion(TDataObject obj)
        {
            if (ReadOnly)
                return;

            lock (m_syncLock)
            {
                Unsubscribe(obj);
                m_dataObjects.Remove(obj);
            }
        }
    }
}
