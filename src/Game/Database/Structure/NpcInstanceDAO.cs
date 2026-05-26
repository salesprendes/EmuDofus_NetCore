using Protocolo.Framework.Database;
using Game.Database.Repository;
namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("npcinstance")]
    public sealed class NpcInstanceDAO : DataAccessObject<NpcInstanceDAO>
    {
        private int _id;
        private int _mapId;
        private int _templateId;
        private int _cellId;
        private int _orientation;
        private int _questionId;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public int MapId
        {
            get => _mapId;
            set => SetProperty(ref _mapId, value);
        }
        public int TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
        }
        public int CellId
        {
            get => _cellId;
            set => SetProperty(ref _cellId, value);
        }
        public int Orientation
        {
            get => _orientation;
            set => SetProperty(ref _orientation, value);
        }
        public int QuestionId
        {
            get => _questionId;
            set => SetProperty(ref _questionId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        private NpcTemplateDAO m_template;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public NpcTemplateDAO Template
        {
            get
            {
                if (m_template == null || m_template.Id != TemplateId)
                    m_template = NpcTemplateRepository.Instance.GetById(TemplateId);
                return m_template;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private MapTemplateDAO m_map;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public MapTemplateDAO Map
        {
            get
            {
                if (m_map == null || m_map.Id != MapId)
                    m_map = MapTemplateRepository.Instance.GetById(MapId);
                return m_map;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private NpcQuestionDAO m_question;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public NpcQuestionDAO Question
        {
            get
            {
                if (m_question == null || m_question.Id != QuestionId)
                    m_question = NpcQuestionRepository.Instance.GetById(QuestionId);
                return m_question;
            }
        }
    }
}

