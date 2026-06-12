using CommunityToolkit.Mvvm.ComponentModel;
using Protocolo.Framework.Generic.Logging;

namespace Protocolo.Framework.Database
{
    public abstract class DataAccessObject<T> : ObservableObject where T : DataAccessObject<T>, new()
    {
        public static ILogger Logger = LogManager.GetLogger(typeof(T));

        public static bool IsRunning
        {
            get;
            set;
        }

        public static SqlManager SqlMgr
        {
            get;
            set;
        }

        [Write(false)]
        public bool IsDirty
        {
            get;
            set;
        }

        [Write(false)]
        public bool IsNew
        {
            get;
            set;
        }

        [Write(false)]
        public bool IsDeleted
        {
            get;
            set;
        }

        static DataAccessObject()
        {
            IsRunning = false;
        }

        public DataAccessObject()
        {
            IsDirty = false;
            IsNew = false;
        }

        public bool Update()
        {
            OnBeforeUpdate();
            return SqlMgr.Update((T)this);
        }

        public bool Delete()
        {
            OnBeforeDelete();
            return SqlMgr.Delete((T)this);
        }

        public bool Insert()
        {
            OnBeforeInsert();
            return SqlMgr.InsertWithKey((T)this);
        }

        [Write(false)]
        public T This
        {
            get
            {
                return (T)this;
            }
        }

        [Write(false)]
        public string DisplayMember
        {
            get
            {
                return ToString();
            }
        }

        public virtual void OnBeforeUpdate()
        {
        }

        public virtual void OnBeforeInsert()
        {
        }

        public virtual void OnBeforeDelete()
        {
        }
    }
}
