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
    [Table("droptemplate")]
    public sealed class DropTemplateDAO : DataAccessObject<DropTemplateDAO>
    {
        private int _id;
        private int _monsterId;
        private string _monsterName;
        private int _templateId;
        private int _ppThreshold;
        private int _max;
        private double _rate;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public int MonsterId
        {
            get => _monsterId;
            set => SetProperty(ref _monsterId, value);
        }
        public string MonsterName
        {
            get => _monsterName;
            set => SetProperty(ref _monsterName, value);
        }
        public int TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
        }
        public int PPThreshold
        {
            get => _ppThreshold;
            set => SetProperty(ref _ppThreshold, value);
        }
        public int Max
        {
            get => _max;
            set => SetProperty(ref _max, value);
        }
        public double Rate
        {
            get => _rate;
            set => SetProperty(ref _rate, value);
        }

        /// <summary>
        /// 
        /// </summary>
        private MonsterDAO m_monster;
        [Write(false)]
        public MonsterDAO Monster
        {
            get
            {
                if (m_monster == null)
                    m_monster = MonsterRepository.Instance.GetById(MonsterId);
                return m_monster;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private ItemTemplateDAO m_item;
        [Write(false)]
        public ItemTemplateDAO ItemTemplate
        {
            get
            {
                if (m_item == null)
                    m_item = ItemTemplateRepository.Instance.GetById(TemplateId);
                return m_item;
            }
        }
    }
}

