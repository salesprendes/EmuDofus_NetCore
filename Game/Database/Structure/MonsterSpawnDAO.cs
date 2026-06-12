using Game.Database.Repository;
using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    public enum ZoneTypeEnum
    {
        TYPE_SUBAREA = 0,
        TYPE_AREA = 1,
        TYPE_SUPERAREA = 2,
        TYPE_MAP = 3,
    }

    [Table("monsterspawn")]
    public sealed class MonsterSpawnDAO : DataAccessObject<MonsterSpawnDAO>
    {
        private int _zoneType;
        private int _zoneId;
        private int _gradeId;
        private double _probability;


        [Key]
        public int ZoneType
        {
            get => _zoneType;
            set => SetProperty(ref _zoneType, value);
        }

        [Write(false)] public ZoneTypeEnum Type => (ZoneTypeEnum)ZoneType;

        public int ZoneId
        {
            get => _zoneId;
            set => SetProperty(ref _zoneId, value);
        }

        public int GradeId
        {
            get => _gradeId;
            set => SetProperty(ref _gradeId, value);
        }

        public double Probability
        {
            get => _probability;
            set => SetProperty(ref _probability, value);
        }

        private MonsterGradeDAO m_grade;

        [Write(false)]
        public MonsterGradeDAO Grade
        {
            get
            {
                if (m_grade == null)
                    m_grade = MonsterGradeRepository.Instance.GetById(GradeId);
                return m_grade;
            }
        }
    }
}

