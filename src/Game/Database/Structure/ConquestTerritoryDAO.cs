using Protocolo.Framework.Database;
using PropertyChanged;
using System.ComponentModel;

namespace Game.Database.Structure
{
    [AddINotifyPropertyChangedInterface]
    [Table("conquest_territory")]
    public class ConquestTerritoryDAO : DataAccessObject<ConquestTerritoryDAO>
    {
        [Key]
        public int SubAreaId { get; set; }
        public int AlignmentId { get; set; }
        public int BonusType { get; set; }
        public int Life { get; set; }
        public int MaxLife { get; set; }
        public int State { get; set; }
        public int PrismMapId { get; set; }
        public int PrismCellId { get; set; }
        public int PrismLevel { get; set; }
        public int PrismHonor { get; set; }
        public int PrismType { get; set; }
    }
}
