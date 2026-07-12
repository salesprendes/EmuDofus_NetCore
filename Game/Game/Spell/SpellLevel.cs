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

        public SpellTemplate Template
        {
            get
            {
                if (m_template == null)
                    m_template = SpellManager.Instance.GetTemplate(SpellId);
                return m_template;
            }
        }

        /// <summary>
        /// Zona (par forma+tamaño) del efecto effectIndex, como la parsea el cliente
        /// (Datacenter/Spell.as): RangeType contiene un par de 2 caracteres POR EFECTO, con las
        /// zonas de los efectos críticos concatenadas tras las normales. Si el dato no trae
        /// suficientes pares, se reutiliza el primero ("Pa" como último recurso).
        /// </summary>
        public string GetEffectZone(int effectIndex, bool critical)
        {
            if (string.IsNullOrEmpty(RangeType))
                return "Pa";

            var index = critical ? (Effects?.Count ?? 0) + effectIndex : effectIndex;
            var offset = index * 2;
            if (offset >= 0 && offset + 2 <= RangeType.Length)
                return RangeType.Substring(offset, 2);

            return RangeType.Length >= 2 ? RangeType.Substring(0, 2) : "Pa";
        }

        /// <summary>
        /// Máscara de objetivos del efecto effectIndex. Los efectos críticos usan el mismo offset
        /// que las zonas (tras los normales); si el dato no la trae, se reutiliza la del índice
        /// sin offset y en última instancia -1 (sin filtro).
        /// </summary>
        public int GetEffectTarget(int effectIndex, bool critical)
        {
            var targets = Template?.Targets;
            if (targets == null || targets.Count == 0)
                return -1;

            var index = critical ? (Effects?.Count ?? 0) + effectIndex : effectIndex;
            if (index >= 0 && index < targets.Count)
                return targets[index];

            return effectIndex >= 0 && effectIndex < targets.Count ? targets[effectIndex] : -1;
        }
    }
}


