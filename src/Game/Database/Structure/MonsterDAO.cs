using Protocolo.Framework.Database;
using System.Collections.Generic;

namespace Game.Database.Structure
{
    [Table("monstruos_template")]
    public sealed class MonsterDAO : DataAccessObject<MonsterDAO>
    {
        private int _id;
        private string _name;
        private int _gfxId;
        private int _skinSize;
        private int _alignment;
        private string _colors;
        private string _kamas;
        private int _aggressionRange;
        private int _race;


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
        public int GfxId
        {
            get => _gfxId;
            set => SetProperty(ref _gfxId, value);
        }
        public int SkinSize
        {
            get => _skinSize;
            set => SetProperty(ref _skinSize, value);
        }
        [Write(false)]
        public int Alignment
        {
            get => _alignment;
            set => SetProperty(ref _alignment, value);
        }
        public string Colors
        {
            get => _colors;
            set => SetProperty(ref _colors, value);
        }
        public string Kamas
        {
            get => _kamas;
            set => SetProperty(ref _kamas, value);
        }
        public int AggressionRange
        {
            get => _aggressionRange;
            set => SetProperty(ref _aggressionRange, value);
        }
        public int Race
        {
            get => _race;
            set => SetProperty(ref _race, value);
        }


        private int m_minKamas = -1, m_maxKamas = -1;
        private List<DropTemplateDAO> m_drops = new List<DropTemplateDAO>();
        private Dictionary<int, MonsterGradeDAO> m_monsterGrades = new Dictionary<int, MonsterGradeDAO>();

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public int MinKamas
        {
            get
            {
                if (m_minKamas == -1)
                    InitKamas();
                return m_minKamas;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public int MaxKamas
        {
            get
            {
                if (m_maxKamas == -1)
                    InitKamas();
                return m_maxKamas;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Write(false)]
        public IEnumerable<MonsterGradeDAO> Grades => m_monsterGrades.Values;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public IEnumerable<DropTemplateDAO> Drops => m_drops;

        /// <summary>
        /// 
        /// </summary>
        private void InitKamas()
        {
            var data = Kamas.Split(';');
            m_minKamas = int.Parse(data[0]);
            m_maxKamas = int.Parse(data[1]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="grade"></param>
        public void AddGrade(MonsterGradeDAO grade)
        {
            m_monsterGrades.Add(grade.Grade, grade);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="drop"></param>
        public void AddDrop(DropTemplateDAO drop)
        {
            m_drops.Add(drop);
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="grade"></param>
        /// <returns></returns>
        public MonsterGradeDAO GetGrade(int grade)
        {
            if (!m_monsterGrades.ContainsKey(grade))
                return null;
            return m_monsterGrades[grade];
        }
    }
}

