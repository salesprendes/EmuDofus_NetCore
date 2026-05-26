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
    [Table("craftentry")]
    public sealed class CraftEntryDAO : DataAccessObject<CraftEntryDAO>
    {
        private int _templateId;
        private int _requiredId;
        private int _requiredQuantity;


        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int RequiredId
        {
            get => _requiredId;
            set => SetProperty(ref _requiredId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int RequiredQuantity
        {
            get => _requiredQuantity;
            set => SetProperty(ref _requiredQuantity, value);
        }

        /// <summary>
        /// 
        /// </summary>
        private ItemTemplateDAO m_requiredTemplate;

        /// <summary>
        /// 
        /// </summary>
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

