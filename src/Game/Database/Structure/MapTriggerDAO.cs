using Protocolo.Framework.Database;
using Game.ActionEffect;
using Game.Condition;
using Game.Entity;
using Game.Spell;
using System;
using System.Collections.Generic;

namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("maptrigger")]
    public sealed class MapTriggerDAO : DataAccessObject<MapTriggerDAO>
    {
        private int _mapId;
        private int _cellId;
        private string _conditions;
        private string _actions;


        public int MapId
        {
            get => _mapId;
            set => SetProperty(ref _mapId, value);
        }
        public int CellId
        {
            get => _cellId;
            set => SetProperty(ref _cellId, value);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool SatisfyConditions(CharacterEntity character)
        {
            return ConditionParser.Instance.Check(Conditions, character);
        }
    }
}


