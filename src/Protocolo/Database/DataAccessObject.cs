using CommunityToolkit.Mvvm.ComponentModel;
using Protocolo.Framework.Generic.Logging;

namespace Protocolo.Framework.Database
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// 
    public abstract class DataAccessObject<T> : ObservableObject where T : DataAccessObject<T>, new()
    {
        /// <summary>
        /// 
        /// </summary>
        public static ILogger Logger = LogManager.GetLogger(typeof(T));

        /// <summary>
        /// 
        /// </summary>
        public static bool IsRunning
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public static SqlManager SqlMgr
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public bool IsDirty
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public bool IsNew
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public bool IsDeleted
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        static DataAccessObject()
        {
            IsRunning = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public DataAccessObject()
        {
            IsDirty = false;
            IsNew = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Update()
        {
            OnBeforeUpdate();
            return SqlMgr.Update((T)this);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Delete()
        {
            OnBeforeDelete();
            return SqlMgr.Delete((T)this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Insert()
        {
            OnBeforeInsert();
            return SqlMgr.InsertWithKey((T)this);
        }

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public T This
        {
            get
            {
                return (T)this;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public string DisplayMember
        {
            get
            {
                return ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnBeforeUpdate()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnBeforeInsert()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnBeforeDelete()
        {
        }
    }
}
