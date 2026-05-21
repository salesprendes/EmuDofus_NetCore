using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("areatemplate")]
    public sealed class AreaDAO : DataAccessObject<AreaDAO>
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public int SuperAreaId
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }
    }
}

