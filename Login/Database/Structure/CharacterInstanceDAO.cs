using Protocolo.Framework.Database;
namespace Login.Database.Structure
{
    [Table("characterinstance")]
    public sealed class CharacterInstanceDAO : DataAccessObject<CharacterInstanceDAO>
    {
        private long _id;
        private int _serverId;
        private long _accountId;
        private string _jobs;


        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int ServerId
        {
            get => _serverId;
            set => SetProperty(ref _serverId, value);
        }

        public long AccountId
        {
            get => _accountId;
            set => SetProperty(ref _accountId, value);
        }

        public string Jobs
        {
            get => _jobs;
            set => SetProperty(ref _jobs, value);
        }
    }
}
