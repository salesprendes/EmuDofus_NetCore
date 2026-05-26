using Protocolo.Framework.Database;
using Game.Spell;
using Game.Stats;
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
    [Table("itemset")]
    public sealed class ItemSetDAO : DataAccessObject<ItemSetDAO>
    {
        private int _id;
        private string _name;
        private string _effects2;
        private string _effects3;
        private string _effects4;
        private string _effects5;
        private string _effects6;
        private string _effects7;
        private string _effects8;


        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Effects2
        {
            get => _effects2;
            set => SetProperty(ref _effects2, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Effects3
        {
            get => _effects3;
            set => SetProperty(ref _effects3, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Effects4
        {
            get => _effects4;
            set => SetProperty(ref _effects4, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Effects5
        {
            get => _effects5;
            set => SetProperty(ref _effects5, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Effects6
        {
            get => _effects6;
            set => SetProperty(ref _effects6, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Effects7
        {
            get => _effects7;
            set => SetProperty(ref _effects7, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Effects8
        {
            get => _effects8;
            set => SetProperty(ref _effects8, value);
        }

        /// <summary>
        /// 
        /// </summary>
        private List<GenericStats> m_statistics;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemCount"></param>
        /// <returns></returns>
        public GenericStats GetStats(int itemCount)
        {
            if(m_statistics == null)
            {
                m_statistics = new List<GenericStats>();
                AddStats(string.Empty); // 0 item
                AddStats(string.Empty); // 1 item
                AddStats(Effects2); 
                AddStats(Effects3);
                AddStats(Effects4);
                AddStats(Effects5);
                AddStats(Effects6);
                AddStats(Effects7);
                AddStats(Effects8);                
            }

            return m_statistics[itemCount];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="effects"></param>
        private void AddStats(string effects)
        {
            var stats = new GenericStats();
            if (effects != string.Empty)
            {
                foreach (var effect in effects.Split(';'))
                {
                    var data = effect.Split(',');
                    var effectId = int.Parse(data[0]);
                    var value = int.Parse(data[1]);
                    stats.AddEffect((EffectEnum)effectId, value);
                }
            }
            m_statistics.Add(stats);
        }
    }
}


