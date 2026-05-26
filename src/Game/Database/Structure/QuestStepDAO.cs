using Protocolo.Framework.Database;
using Game.ActionEffect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    [Table("queststep")]
    public sealed class QuestStepDAO : DataAccessObject<QuestStepDAO>
    {
        private int _id;
        private int _questId;
        private int _order;
        private string _name;
        private string _description;
        private string _actions;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public int QuestId
        {
            get => _questId;
            set => SetProperty(ref _questId, value);
        }
        public int Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
        public string Actions
        {
            get => _actions;
            set => SetProperty(ref _actions, value);
        }

        /// <summary>
        /// 
        /// </summary>
        private ActionList m_actions;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        public ActionList ActionsList
        {
            get
            {
                if (m_actions == null)                
                    m_actions = ActionList.Deserialize(Actions);                
                return m_actions;
            }
        }

        public List<QuestObjectiveDAO> Objectives { get; } = new List<QuestObjectiveDAO>();
    }
}


