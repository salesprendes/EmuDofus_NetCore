using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("subareatemplate")]
    public sealed class SubAreaDAO : DataAccessObject<SubAreaDAO>
    {
        private int _id;
        private int _areaId;
        private string _name;
        private int _canConquest;
        private int _defaultAlignment;
        private int _premiumZone;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int AreaId
        {
            get => _areaId;
            set => SetProperty(ref _areaId, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int CanConquest
        {
            get => _canConquest;
            set => SetProperty(ref _canConquest, value);
        }

        public int DefaultAlignment
        {
            get => _defaultAlignment;
            set => SetProperty(ref _defaultAlignment, value);
        }

        public int PremiumZone
        {
            get => _premiumZone;
            set => SetProperty(ref _premiumZone, value);
        }
    }
}

