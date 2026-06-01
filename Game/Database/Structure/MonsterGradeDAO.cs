using Protocolo.Framework.Database;
using Game.Database.Repository;
using Game.Entity;
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
    [Table("monstergrade")]
    public sealed class MonsterGradeDAO : DataAccessObject<MonsterGradeDAO>
    {
        private long _id;
        private int _monsterId;
        private int _grade;
        private int _level;
        private int _ap;
        private int _mp;
        private int _maxLife;
        private int _neutralResistance;
        private int _earthResistance;
        private int _fireResistance;
        private int _waterResistance;
        private int _airResistance;
        private int _apDodgePercent;
        private int _mpDodgePercent;
        private int _wisdom;
        private int _strenght;
        private int _intelligence;
        private int _chance;
        private int _agility;
        private int _initiative;
        private int _maxInvocation;
        private int _experience;


        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int MonsterId
        {
            get => _monsterId;
            set => SetProperty(ref _monsterId, value);
        }

        public int Grade
        {
            get => _grade;
            set => SetProperty(ref _grade, value);
        }
        
        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }
        public int AP
        {
            get => _ap;
            set => SetProperty(ref _ap, value);
        }
        public int MP
        {
            get => _mp;
            set => SetProperty(ref _mp, value);
        }
        public int MaxLife
        {
            get => _maxLife;
            set => SetProperty(ref _maxLife, value);
        }
        public int NeutralResistance
        {
            get => _neutralResistance;
            set => SetProperty(ref _neutralResistance, value);
        }
        public int EarthResistance
        {
            get => _earthResistance;
            set => SetProperty(ref _earthResistance, value);
        }
        public int FireResistance
        {
            get => _fireResistance;
            set => SetProperty(ref _fireResistance, value);
        }
        public int WaterResistance
        {
            get => _waterResistance;
            set => SetProperty(ref _waterResistance, value);
        }
        public int AirResistance
        {
            get => _airResistance;
            set => SetProperty(ref _airResistance, value);
        }
        public int APDodgePercent
        {
            get => _apDodgePercent;
            set => SetProperty(ref _apDodgePercent, value);
        }
        public int MPDodgePercent
        {
            get => _mpDodgePercent;
            set => SetProperty(ref _mpDodgePercent, value);
        }
        public int Wisdom
        {
            get => _wisdom;
            set => SetProperty(ref _wisdom, value);
        }
        public int Strenght
        {
            get => _strenght;
            set => SetProperty(ref _strenght, value);
        }
        public int Intelligence
        {
            get => _intelligence;
            set => SetProperty(ref _intelligence, value);
        }
        public int Chance
        {
            get => _chance;
            set => SetProperty(ref _chance, value);
        }
        public int Agility
        {
            get => _agility;
            set => SetProperty(ref _agility, value);
        }
        public int Initiative
        {
            get => _initiative;
            set => SetProperty(ref _initiative, value);
        }
        public int MaxInvocation
        {
            get => _maxInvocation;
            set => SetProperty(ref _maxInvocation, value);
        }
        public int Experience
        {
            get => _experience;
            set => SetProperty(ref _experience, value);
        }        
        /// <summary>
        /// 
        /// </summary>
        private MonsterDAO m_template;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Write(false)]
        public MonsterDAO Template
        {
            get
            {
                if (m_template == null)
                    m_template = MonsterRepository.Instance.GetById(MonsterId);
                return m_template;
            }
        }
    }
}


