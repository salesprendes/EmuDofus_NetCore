using Protocolo.Framework.Database;
using Game.ActionEffect;
using Game.Fight;
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
    [Table("fightaction")]
    public sealed class FightActionDAO : DataAccessObject<FightActionDAO>
    {
        private int _zoneType;
        private int _zoneId;
        private int _fightType;
        private int _fightState;
        private string _conditions;
        private string _actions;


        [Key]
        public int ZoneType
        {
            get => _zoneType;
            set => SetProperty(ref _zoneType, value);
        }

        [Write(false)]
        public ZoneTypeEnum Zone => (ZoneTypeEnum)ZoneType;

        [Key]
        public int ZoneId
        {
            get => _zoneId;
            set => SetProperty(ref _zoneId, value);
        }

        [Key]
        public int FightType
        {
            get => _fightType;
            set => SetProperty(ref _fightType, value);
        }

        [Write(false)]
        public FightTypeEnum Fight => (FightTypeEnum)FightType;

        [Key]
        public int FightState
        {
            get => _fightState;
            set => SetProperty(ref _fightState, value);
        }

        [Write(false)]
        public FightStateEnum State => (FightStateEnum)FightState;

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


