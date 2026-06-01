using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("superareatemplate")]
    public sealed class SuperAreaDAO : DataAccessObject<SuperAreaDAO>
    {
        private int _id;
        private string _name;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}

