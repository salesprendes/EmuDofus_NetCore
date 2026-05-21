using System;
using Protocolo.Framework.Database;
using PropertyChanged;

namespace Login.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("account")]
    [AddINotifyPropertyChangedInterface]
    public sealed class AccountDAO : DataAccessObject<AccountDAO>
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long Id
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Pseudo
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Password
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Power
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime CreationDate
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime LastConnectionDate
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string LastConnectionIP
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime RemainingSubscription
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public bool Banned
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string Question
        {
            get;
            set;
        }
    }
}

