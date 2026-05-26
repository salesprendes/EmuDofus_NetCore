using Protocolo.Framework.Database;
using Game.ActionEffect;
using Game.Spell;
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
    [Table("npcresponse")]
    public sealed class NpcResponseDAO : DataAccessObject<NpcResponseDAO>
    {
        private int _id;
        private string _conditions;
        private string _actions;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Conditions
        {
            get => _conditions;
            set => SetProperty(ref _conditions, value);
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
                {
                    m_actions = ActionList.Deserialize(Actions);
                }
                return m_actions;
            }
        }
    }
}


