using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("areatemplate")]
    public sealed class AreaDAO : DataAccessObject<AreaDAO>
    {
        private int _id;
        private int _superAreaId;
        private string _name;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int SuperAreaId
        {
            get => _superAreaId;
            set => SetProperty(ref _superAreaId, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}

