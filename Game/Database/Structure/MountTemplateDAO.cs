using Protocolo.Framework.Database;
using Game.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    [Table("mounttemplate")]
    public sealed class MountTemplateDAO : DataAccessObject<MountTemplateDAO>
    {
        private int _id;
        private string _name;
        private string _effects;
        private int _defaultPods;
        private int _podsPerLevel;
        private int _defaultEnergy;
        private int _energyPerLevel;
        private int _maxMaturity;
        private int _gestationTime;
        private int _learningTime;


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
        public string Effects
        {
            get => _effects;
            set => SetProperty(ref _effects, value);
        }
        public int DefaultPods
        {
            get => _defaultPods;
            set => SetProperty(ref _defaultPods, value);
        }
        public int PodsPerLevel
        {
            get => _podsPerLevel;
            set => SetProperty(ref _podsPerLevel, value);
        }
        public int DefaultEnergy
        {
            get => _defaultEnergy;
            set => SetProperty(ref _defaultEnergy, value);
        }
        public int EnergyPerLevel
        {
            get => _energyPerLevel;
            set => SetProperty(ref _energyPerLevel, value);
        }
        public int MaxMaturity
        {
            get => _maxMaturity;
            set => SetProperty(ref _maxMaturity, value);
        }
        public int GestationTime
        {
            get => _gestationTime;
            set => SetProperty(ref _gestationTime, value);
        }
        public int LearningTime
        {
            get => _learningTime;
            set => SetProperty(ref _learningTime, value);
        }
        
        /// <summary>
        /// 
        /// </summary>
        private RandomStatistics m_effects;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public RandomStatistics RandomEffects
        {
            get
            {
                if (m_effects == null)
                    m_effects = RandomStatistics.Deserialize(Effects);
                return m_effects;
            }
        }
    }
}


