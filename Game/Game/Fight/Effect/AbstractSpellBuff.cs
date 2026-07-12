using Game.Network;
using Game.Spell;
using System;

namespace Game.Fight.Effect
{
    public enum ActiveType
    {
        ACTIVE_STATS,
        ACTIVE_BEGINTURN,
        ACTIVE_ENDTURN,
        ACTIVE_ENDMOVE,
        ACTIVE_ATTACK_BEFORE_JET,
        ACTIVE_ATTACK_AFTER_JET,
        ACTIVE_ATTACKED_BEFORE_JET,
        ACTIVE_ATTACKED_AFTER_JET,
    }

    public enum DecrementType
    {
        TYPE_BEGINTURN,
        TYPE_ENDTURN,
        TYPE_ENDMOVE,
    }

    public abstract class AbstractSpellBuff
    {
        public DecrementType DecrementType
        {
            get;
            set;
        }

        public ActiveType ActiveType
        {
            get;
            set;
        }


        public CastInfos CastInfos
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

        public int Duration
        {
            get;
            set;
        }

        public bool IsDebuffable
        {
            get
            {
                switch (CastInfos.SubEffect)
                {
                    case EffectEnum.ESTADO_MAS:
                    case EffectEnum.APARIENCIA_CAMBIAR:
                    case EffectEnum.CASTIGO_MAS:
                    case EffectEnum.CASTIGO_EROSION: // la contrapartida del castigo tampoco se debufea
                        return false;
                }

                return true;
            }
        }

        public AbstractSpellBuff(CastInfos castInfos, AbstractFighter target, ActiveType activeType, DecrementType decrementType)
        {
            CastInfos = castInfos;
            // Duración negativa en los datos ("-1") = dura todo el combate.
            var baseDuration = castInfos.Duration < 0 ? 1000 : castInfos.Duration;
            // El +1 solo aplica a buffs que decrementan al FINAL del turno: si se lanzan durante
            // el propio turno del objetivo, sin él expirarían antes de que vuelva a jugar. Los que
            // decrementan al inicio ya cuentan el turno correcto (aplicarles +1 duplicaba turnos,
            // p.ej. TurnPassBuff hacía pasar dos turnos seguidos).
            Duration = (decrementType == DecrementType.TYPE_ENDTURN && target.Fight.CurrentFighter == target)
                ? baseDuration + 1
                : baseDuration;
            Caster = castInfos.Caster;
            Target = target;

            ActiveType = activeType;
            DecrementType = decrementType;

            switch (castInfos.EffectType)
            {
                case EffectEnum.DEFENSA_DEVOLVER_HECHIZO:
                    Target.Fight.Dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType, Target.Id, CastInfos.Value2.ToString(), CastInfos.Value2.ToString(), "10", CastInfos.Value2.ToString(), CastInfos.Duration.ToString(), CastInfos.SpellId.ToString()));
                    break;

                case EffectEnum.COMBATE_SUERTE_ECAFLIP:
                case EffectEnum.CASTIGO_MAS:
                    Target.Fight.Dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType, Target.Id, CastInfos.Value1.ToString(), CastInfos.Value2.ToString(), CastInfos.Value3.ToString(), "", CastInfos.Duration.ToString(), CastInfos.SpellId.ToString()));
                    break;

                case EffectEnum.PANDA_CARGAR:
                    break;

                default:
                    Target.Fight.Dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType, Target.Id, CastInfos.Value1.ToString(), "", "", "", CastInfos.Duration.ToString(), CastInfos.SpellId.ToString()));
                    break;
            }
        }

        public void SendTo(Action<string> dispatch)
        {
            switch (CastInfos.EffectType)
            {
                case EffectEnum.DEFENSA_DEVOLVER_HECHIZO:
                    dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType, Target.Id, CastInfos.Value2.ToString(), CastInfos.Value2.ToString(), "10", CastInfos.Value2.ToString(), Duration.ToString(), CastInfos.SpellId.ToString()));
                    break;

                case EffectEnum.COMBATE_SUERTE_ECAFLIP:
                case EffectEnum.CASTIGO_MAS:
                    dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType, Target.Id, CastInfos.Value1.ToString(), CastInfos.Value2.ToString(), CastInfos.Value3.ToString(), "", Duration.ToString(), CastInfos.SpellId.ToString()));
                break;

                case EffectEnum.PANDA_CARGAR:
                    break;

                default:
                    dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType, Target.Id, CastInfos.Value1.ToString(), "", "", "", Duration.ToString(), CastInfos.SpellId.ToString()));
                    break;
            }
        }

        public virtual FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            // El objetivo sigue en el combate; el lanzador puede haberlo abandonado (Fight null).
            return Target.Fight?.TryKillFighter(Target, Caster) ?? FightActionResultEnum.RESULT_NOTHING;
        }

        public virtual FightActionResultEnum RemoveEffect()
        {
            return Target.Fight?.TryKillFighter(Target, Caster) ?? FightActionResultEnum.RESULT_NOTHING;
        }

        public int DecrementDuration()
        {
            Duration--;
            CastInfos.FakeValue = 0;

            return Duration;
        }
    }
}


