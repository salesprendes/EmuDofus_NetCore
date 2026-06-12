using System;
using Protocolo.Framework.Database;
namespace Login.Database.Structure
{
    [Table("account")]
    public sealed class AccountDAO : DataAccessObject<AccountDAO>
    {
        private long _id;
        private string _name;
        private string _pseudo;
        private string _password;
        private int _power;
        private DateTime _creationDate;
        private DateTime _lastConnectionDate;
        private string _lastConnectionIP;
        private DateTime _remainingSubscription;
        private bool _banned;
        private string _question;


        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Pseudo
        {
            get => _pseudo;
            set => SetProperty(ref _pseudo, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public int Power
        {
            get => _power;
            set => SetProperty(ref _power, value);
        }

        public DateTime CreationDate
        {
            get => _creationDate;
            set => SetProperty(ref _creationDate, value);
        }

        public DateTime LastConnectionDate
        {
            get => _lastConnectionDate;
            set => SetProperty(ref _lastConnectionDate, value);
        }

        public string LastConnectionIP
        {
            get => _lastConnectionIP;
            set => SetProperty(ref _lastConnectionIP, value);
        }

        public DateTime RemainingSubscription
        {
            get => _remainingSubscription;
            set => SetProperty(ref _remainingSubscription, value);
        }

        public bool Banned
        {
            get => _banned;
            set => SetProperty(ref _banned, value);
        }

        public string Question
        {
            get => _question;
            set => SetProperty(ref _question, value);
        }
    }
}

