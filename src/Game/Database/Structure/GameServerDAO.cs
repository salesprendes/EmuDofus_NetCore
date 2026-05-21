using Protocolo.Framework.Database;
using PropertyChanged;

namespace Game.Database.Structure
{
    [Table("gameservers")]
    [AddINotifyPropertyChangedInterface]
    public sealed class GameServerDAO : DataAccessObject<GameServerDAO>
    {
        [Key]
        public int Id { get; set; }
        public int Port { get; set; }
        public int State { get; set; }
        public int Sub { get; set; }
        public int FreePlaces { get; set; }
        public string Ip { get; set; }
    }
}
