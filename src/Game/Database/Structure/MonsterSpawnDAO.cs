using Protocolo.Framework.Database;
using Game.Database.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    public enum ZoneTypeEnum
    {
        TYPE_SUBAREA = 0,
        TYPE_AREA = 1,
        TYPE_SUPERAREA = 2,
        TYPE_MAP = 3,
    }

    /// <summary>
    /// 
    /// </summary>
    [Table("monsterspawn")]
    public sealed class MonsterSpawnDAO : DataAccessObject<MonsterSpawnDAO>
    {
        private int _zoneType;
        private int _zoneId;
        private int _gradeId;
        private double _probability;


        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int ZoneType
        {
            get => _zoneType;
            set => SetProperty(ref _zoneType, value);
        }

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public ZoneTypeEnum Type => (ZoneTypeEnum)ZoneType;

        /// <summary>
        /// 
        /// </summary>
        public int ZoneId
        {
            get => _zoneId;
            set => SetProperty(ref _zoneId, value);
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        private MonsterGradeDAO m_grade;

        /// <summary>
        /// 
        /// </summary>
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

