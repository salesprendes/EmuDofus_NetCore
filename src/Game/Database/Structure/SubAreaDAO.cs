using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("subareatemplate")]
    public sealed class SubAreaDAO : DataAccessObject<SubAreaDAO>
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public int AreaId
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public int CanConquest
        {
            get;
            set;
        }

        public int DefaultAlignment
        {
            get;
            set;
        }

        public int PremiumZone
        {
            get;
            set;
        }
    }
}

