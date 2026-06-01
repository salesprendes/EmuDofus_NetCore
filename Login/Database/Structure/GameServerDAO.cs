using Protocolo.Framework.Database;
namespace Login.Database.Structure
{
    [Table("gameservers")]
    public sealed class GameServerDAO : DataAccessObject<GameServerDAO>
    {
        private int _id;
        private int _port;
        private int _state;
        private int _sub;
        private int _freePlaces;
        private string _ip;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }
        public int State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }
        public int Sub
        {
            get => _sub;
            set => SetProperty(ref _sub, value);
        }
        public int FreePlaces
        {
            get => _freePlaces;
            set => SetProperty(ref _freePlaces, value);
        }
        public string Ip
        {
            get => _ip;
            set => SetProperty(ref _ip, value);
        }
    }
}
