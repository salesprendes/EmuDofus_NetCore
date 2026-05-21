using Protocolo.Framework.Database;
using Game.ActionEffect;
using Game.Spell;
using PropertyChanged;
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
        [Key]
        public int Id
        {
            get;
            set;
        }

        public string Conditions
        {
            get;
            set;
        }

        public string Actions
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private ActionList m_actions;

        /// <summary>
        /// 
        /// </summary>
        [Write(false)]
        [DoNotNotify]
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


