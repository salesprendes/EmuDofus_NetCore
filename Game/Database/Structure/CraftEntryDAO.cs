using Game.Database.Repository;
using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("craftentry")]
    public sealed class CraftEntryDAO : DataAccessObject<CraftEntryDAO>
    {
        private int _templateId;
        private int _requiredId;
        private int _requiredQuantity;


        [Key]
        public int TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
        }

        [Key]
        public int RequiredId
        {
            get => _requiredId;
            set => SetProperty(ref _requiredId, value);
        }

        public int RequiredQuantity
        {
            get => _requiredQuantity;
            set => SetProperty(ref _requiredQuantity, value);
        }

        private ItemTemplateDAO m_requiredTemplate;

        [Write(false)]
        public ItemTemplateDAO RequiredTemplate
        {
            get
            {
                if (m_requiredTemplate == null)
                    m_requiredTemplate = ItemTemplateRepository.Instance.GetById(RequiredId);
                return m_requiredTemplate;
            }
        }
    }
}

