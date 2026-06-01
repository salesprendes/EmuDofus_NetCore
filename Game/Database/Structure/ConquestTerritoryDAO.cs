using Protocolo.Framework.Database;
using System.ComponentModel;

namespace Game.Database.Structure
{
    [Table("conquest_territory")]
    public class ConquestTerritoryDAO : DataAccessObject<ConquestTerritoryDAO>
    {
        private int _subAreaId;
        private int _alignmentId;
        private int _bonusType;
        private int _life;
        private int _maxLife;
        private int _state;
        private int _prismMapId;
        private int _prismCellId;
        private int _prismLevel;
        private int _prismHonor;
        private int _prismType;


        [Key]
        public int SubAreaId
        {
            get => _subAreaId;
            set => SetProperty(ref _subAreaId, value);
        }
        public int AlignmentId
        {
            get => _alignmentId;
            set => SetProperty(ref _alignmentId, value);
        }
        public int BonusType
        {
            get => _bonusType;
            set => SetProperty(ref _bonusType, value);
        }
        public int Life
        {
            get => _life;
            set => SetProperty(ref _life, value);
        }
        public int MaxLife
        {
            get => _maxLife;
            set => SetProperty(ref _maxLife, value);
        }
        public int State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }
        public int PrismMapId
        {
            get => _prismMapId;
            set => SetProperty(ref _prismMapId, value);
        }
        public int PrismCellId
        {
            get => _prismCellId;
            set => SetProperty(ref _prismCellId, value);
        }
        public int PrismLevel
        {
            get => _prismLevel;
            set => SetProperty(ref _prismLevel, value);
        }
        public int PrismHonor
        {
            get => _prismHonor;
            set => SetProperty(ref _prismHonor, value);
        }
        public int PrismType
        {
            get => _prismType;
            set => SetProperty(ref _prismType, value);
        }
    }
}
