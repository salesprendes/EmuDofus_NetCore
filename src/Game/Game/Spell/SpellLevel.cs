using Game.Manager;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Game.Spell
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Serializable]
    public sealed class SpellLevel
    {
        public int SpellId;
        public int Level;
        public int APCost;
        public int MinPO;
        public int MaxPO;
        public int CSRate;
        public int ECSRate;
        public bool InLine;
        public bool LOS;
        public bool EmptyCell;
        public bool AllowPOBoost;
        public int MaxLaunchPerGame;
        public int MaxLaunchPerTurn;
        public int MaxLaunchPerTarget;
        public int Cooldown;
        public int RequiredLevel;
        public int IsECSEndTurn;
        public string RangeType;
        public List<int> Conditions;
        public List<int> TargetZones;
        public List<SpellEffect> Effects;
        public List<SpellEffect> CriticalEffects;

        [ProtoIgnore]
        [NonSerialized]
        private SpellTemplate m_template;

        /// <summary>
        /// 
        /// </summary>
        public SpellTemplate Template
        {
            get
            {
                if (m_template == null)
                    m_template = SpellManager.Instance.GetTemplate(SpellId);
                return m_template;
            }
        }
    }
}


