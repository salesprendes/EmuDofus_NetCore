using Protocolo.Framework.Database;
using Game.Database.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    [Table("mountinstance")]
    public sealed class MountDAO : DataAccessObject<MountDAO>
    {
        private long _id;
        private int _templateId;
        private long _ownerId;
        private int _paddockId;
        private bool _wild;
        private bool _castrated;
        private int _tired;
        private bool _sex;
        private int _capacity;
        private string _name;
        private long _experience;
        private long _stamina;
        private long _maturity;
        private long _energy;
        private long _serenity;
        private long _love;
        private int _reproduction;
        private int _xpSharePercent;


        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public int TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
        }
        public long OwnerId
        {
            get => _ownerId;
            set => SetProperty(ref _ownerId, value);
        }
        public int PaddockId
        {
            get => _paddockId;
            set => SetProperty(ref _paddockId, value);
        }
        public bool Wild
        {
            get => _wild;
            set => SetProperty(ref _wild, value);
        }
        public bool Castrated
        {
            get => _castrated;
            set => SetProperty(ref _castrated, value);
        }
        public int Tired
        {
            get => _tired;
            set => SetProperty(ref _tired, value);
        }
        public bool Sex
        {
            get => _sex;
            set => SetProperty(ref _sex, value);
        }
        public int Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public long Experience
        {
            get => _experience;
            set => SetProperty(ref _experience, value);
        }
        public long Stamina
        {
            get => _stamina;
            set => SetProperty(ref _stamina, value);
        }
        public long Maturity
        {
            get => _maturity;
            set => SetProperty(ref _maturity, value);
        }
        public long Energy
        {
            get => _energy;
            set => SetProperty(ref _energy, value);
        }
        public long Serenity
        {
            get => _serenity;
            set => SetProperty(ref _serenity, value);
        }
        public long Love
        {
            get => _love;
            set => SetProperty(ref _love, value);
        }
        public int Reproduction
        {
            get => _reproduction;
            set => SetProperty(ref _reproduction, value);
        }
        public int XPSharePercent
        {
            get => _xpSharePercent;
            set => SetProperty(ref _xpSharePercent, value);
        }

        private MountTemplateDAO m_template;

        [Write(false)]
        public MountTemplateDAO Template
        {
            get
            {
                if(m_template == null)
                    m_template = MountTemplateRepository.Instance.GetById(TemplateId);
                return m_template;
            }
        }
    }
}

