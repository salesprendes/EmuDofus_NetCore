using Game.Map;
using Game.Spell;

namespace Game.Fight.Effect
{
    public sealed class CastInfos
    {
        public static bool IsMalusEffect(EffectEnum effectType)
        {
            return !IsDamageEffect(effectType) && !IsBonusEffect(effectType) && !IsFriendlyEffect(effectType) && !IsSpecial(effectType);
        }

        public static bool IsSpecial(EffectEnum effectType)
        {
            switch (effectType)
            {
                case EffectEnum.COMBATE_COLOCAR_TRAMPA:
                case EffectEnum.COMBATE_COLOCAR_GLIFO:
                case EffectEnum.INVOCACION_CRIATURA:
                case EffectEnum.INVOCACION_DOBLE:
                case EffectEnum.MOVIMIENTO_TELETRANSPORTAR:
                    return true;
            }
            return false;
        }

        public static bool IsFriendlyEffect(EffectEnum effectType)
        {
            switch (effectType)
            {
                case EffectEnum.COMBATE_SACRIFICIO:
                case EffectEnum.DEFENSA_EVASION:
                case EffectEnum.MOVIMIENTO_INTERCAMBIAR_POSICION:
                case EffectEnum.PANDA_CARGAR:
                case EffectEnum.PANDA_LANZAR:
                case EffectEnum.DEFENSA_DEVOLVER_HECHIZO:
                    return true;
            }

            return false;
        }

        public static bool IsDamageEffect(EffectEnum effectType)
        {
            switch (effectType)
            {
                case EffectEnum.ROBO_VIDA_TIERRA:
                case EffectEnum.ROBO_VIDA_FUEGO:
                case EffectEnum.ROBO_VIDA_AGUA:
                case EffectEnum.ROBO_VIDA_AIRE:
                case EffectEnum.ROBO_VIDA_NEUTRAL:
                case EffectEnum.DANO_TIERRA:
                case EffectEnum.DANO_NEUTRAL:
                case EffectEnum.DANO_FUEGO:
                case EffectEnum.DANO_AGUA:
                case EffectEnum.DANO_AIRE:
                case EffectEnum.DANO_BRUTO:
                case EffectEnum.DANO_VIDA_AIRE:
                case EffectEnum.DANO_VIDA_TIERRA:
                case EffectEnum.DANO_VIDA_FUEGO:
                case EffectEnum.DANO_VIDA_NEUTRAL:
                case EffectEnum.DANO_VIDA_AGUA:
                case EffectEnum.DANO_POR_PA:
                case EffectEnum.ROBO_VIDA_FIJO:
                    return true;
            }

            return false;
        }

        public static bool IsBonusEffect(EffectEnum effectType)
        {
            switch (effectType)
            {
                case EffectEnum.CURACION:
                case EffectEnum.STAT_MAS_AGILIDAD:
                case EffectEnum.STAT_MAS_PA:
                case EffectEnum.STAT_MAS_PA_BIS:
                case EffectEnum.STAT_MAS_ESQUIVA_PA:
                case EffectEnum.STAT_MAS_ARMADURA:
                case EffectEnum.STAT_MAS_ARMADURA_AIRE:
                case EffectEnum.STAT_MAS_ARMADURA_BIS:
                case EffectEnum.STAT_MAS_ARMADURA_TIERRA:
                case EffectEnum.STAT_MAS_ARMADURA_FUEGO:
                case EffectEnum.STAT_MAS_ARMADURA_NEUTRAL:
                case EffectEnum.STAT_MAS_ARMADURA_AGUA:
                case EffectEnum.CARACTERISTICA_MAS_AGILIDAD:
                case EffectEnum.CARACTERISTICA_MAS_INTELIGENCIA:
                case EffectEnum.CARACTERISTICA_MAS_PUNTOS:
                case EffectEnum.CARACTERISTICA_MAS_FUERZA:
                case EffectEnum.CARACTERISTICA_MAS_VITALIDAD:
                case EffectEnum.CARACTERISTICA_MAS_SABIDURIA:
                case EffectEnum.STAT_MAS_SUERTE:
                case EffectEnum.CASTIGO_MAS:
                case EffectEnum.STAT_MAS_DANO:
                case EffectEnum.STAT_MAS_DANO_CRITICO:
                case EffectEnum.STAT_MAS_DANO_MAGICO:
                case EffectEnum.STAT_MAS_DANO_PORCENTAJE:
                case EffectEnum.STAT_MAS_DANO_FISICO:
                case EffectEnum.STAT_MAS_DANO_TRAMPA:
                case EffectEnum.STAT_MAS_CURAS:
                case EffectEnum.STAT_MAS_INICIATIVA:
                case EffectEnum.STAT_MAS_INTELIGENCIA:
                case EffectEnum.STAT_MAS_INVOCACIONES_MAX:
                case EffectEnum.STAT_MAS_VIDA:
                case EffectEnum.STAT_MAS_PM:
                case EffectEnum.STAT_MAS_PM_BONUS:
                case EffectEnum.STAT_MAS_ESQUIVA_PM:
                case EffectEnum.STAT_MAS_ALCANCE:
                case EffectEnum.STAT_MAS_PODS:
                case EffectEnum.STAT_MAS_PROSPECCION:
                case EffectEnum.STAT_MAS_RESISTENCIA_AIRE:
                case EffectEnum.STAT_MAS_RESISTENCIA_TIERRA:
                case EffectEnum.STAT_MAS_RESISTENCIA_FUEGO:
                case EffectEnum.STAT_MAS_REDUCCION_DANO_MAGICO:
                case EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA:
                case EffectEnum.STAT_MAS_REDUCCION_DANO_FISICO:
                case EffectEnum.STAT_MAS_RESISTENCIA_AGUA:
                case EffectEnum.STAT_MAS_DANO_DEVUELTO:
                case EffectEnum.ESTADO_MAS:
                case EffectEnum.STAT_MAS_FUERZA:
                case EffectEnum.STAT_MAS_VITALIDAD:
                case EffectEnum.STAT_MAS_SABIDURIA:
                case EffectEnum.STAT_MAESTRIA:
                case EffectEnum.STAT_MULTIPLICAR_DANO:
                case EffectEnum.HECHIZO_MAS_DANO:
                    return true;
            }

            return false;
        }

        public EffectEnum EffectType
        {
            get;
            set;
        }

        public EffectEnum SubEffect
        {
            get;
            set;
        }

        public int SpellId
        {
            get;
            set;
        }

        public int CellId
        {
            get;
            set;
        }

        public bool IsReflect
        {
            get;
            set;
        }

        public bool IsPoison
        {
            get;
            set;
        }

        public bool IsReturnedDamages
        {
            get;
            set;
        }

        public bool IsMelee
        {
            get;
            set;
        }

        public bool IsTrap
        {
            get;
            set;
        }

        public int RandomJet => (Value2 < Value1 ? Value1 : Util.Next(Value1, Value2 + 1));

        public int Value1
        {
            get;
            set;
        }

        public int Value2
        {
            get;
            set;
        }

        public int Value3
        {
            get;
            set;
        }

        public int FakeValue
        {
            get;
            set;
        }

        public int DamageValue
        {
            get;
            set;
        }

        public int Chance
        {
            get;
            set;
        }

        public int Duration
        {
            get;
            set;
        }

        public int SpellLevel
        {
            get;
            set;
        }

        public string RangeType
        {
            get;
            set;
        }

        public AbstractFighter Caster
        {
            get;
            set;
        }

        public AbstractFighter Target
        {
            get;
            set;
        }

        public MapInstance Map
        {
            get;
            set;
        }

        public AbstractFight Fight
        {
            get;
            set;
        }

        public int TargetKnownCellId
        {
            get;
            set;
        }

        public CastInfos(EffectEnum effectType, int spellId, int cellId, int value1, int value2, int value3, int chance, int duration, AbstractFighter caster, AbstractFighter target, string rangeType = "", int targetKnownCellId = 0, int spellLevel = -1, bool isMelee = false, bool isTrap = false, EffectEnum subEffect = EffectEnum.NINGUNO, int damageValue = 0, int fakeValue = 0)
        {
            Fight = caster.Fight;
            Map = caster.Fight.Map;
            SpellLevel = spellLevel;
            TargetKnownCellId = targetKnownCellId;
            FakeValue = fakeValue;
            RangeType = rangeType;
            EffectType = effectType;
            SpellId = spellId;
            CellId = cellId;
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
            Chance = chance;
            IsTrap = isTrap;
            Duration = duration;
            Caster = caster;
            Target = target;

            if (subEffect == EffectEnum.NINGUNO)
            {
                SubEffect = effectType;
            }
            else
            {
                SubEffect = subEffect;
            }

            DamageValue = damageValue;
            IsMelee = isMelee;
        }
    }
}


