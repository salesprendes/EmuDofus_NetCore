using Game.Fight.Effect.Type;
using Game.Spell;
using Protocolo.Framework.Generic;
using System.Collections.Generic;

namespace Game.Fight.Effect
{
    public sealed class EffectManager : Singleton<EffectManager>
    {
        private readonly Dictionary<EffectEnum, AbstractSpellEffect> m_effects;

        public EffectManager()
        {
            m_effects = new Dictionary<EffectEnum, AbstractSpellEffect>
            {
                { EffectEnum.DANO_PROPIO, new SelfDamageEffect() },
                { EffectEnum.DANO_TIERRA, new DamageEffect() },
                { EffectEnum.DANO_NEUTRAL, new DamageEffect() },
                { EffectEnum.DANO_FUEGO, new DamageEffect() },
                { EffectEnum.DANO_AGUA, new DamageEffect() },
                { EffectEnum.DANO_AIRE, new DamageEffect() },
                { EffectEnum.DANO_VIDA_NEUTRAL, new DamageLifePercentEffect(EffectEnum.DANO_BRUTO) },
                { EffectEnum.DANO_VIDA_AIRE, new DamageLifePercentEffect(EffectEnum.DANO_AIRE) },
                { EffectEnum.DANO_VIDA_TIERRA, new DamageLifePercentEffect(EffectEnum.DANO_TIERRA) },
                { EffectEnum.DANO_VIDA_FUEGO, new DamageLifePercentEffect(EffectEnum.DANO_FUEGO) },
                { EffectEnum.DANO_VIDA_AGUA, new DamageLifePercentEffect(EffectEnum.DANO_AGUA) },
                { EffectEnum.DANO_ENTREGA_VIDA, new DropLifeEffect() },
                { EffectEnum.DANO_PUNICION, new PunishmentDamageEffect() },
                { EffectEnum.DEFENSA_DEVOLVER_HECHIZO, new ReflectSpellEffect() },
                { EffectEnum.ROBO_VIDA_FIJO, new PureLifeStealEffect() },
                { EffectEnum.DANO_POR_PA, new DamagePerAPEffect() },


                { EffectEnum.ROBO_VIDA_NEUTRAL, new LifeStealEffect() },
                { EffectEnum.ROBO_VIDA_TIERRA, new LifeStealEffect() },
                { EffectEnum.ROBO_VIDA_FUEGO, new LifeStealEffect() },
                { EffectEnum.ROBO_VIDA_AGUA, new LifeStealEffect() },
                { EffectEnum.ROBO_VIDA_AIRE, new LifeStealEffect() },


                { EffectEnum.CURACION, new HealEffect() },


                { EffectEnum.MOVIMIENTO_TELETRANSPORTAR, new TeleportEffect() },


                { EffectEnum.STAT_MAS_ARMADURA, new ArmorEffect() },
                { EffectEnum.STAT_MAS_ARMADURA_BIS, new ArmorEffect() },


                { EffectEnum.STAT_MAS_PA, new StatsEffect() },
                { EffectEnum.STAT_MAS_PA_BIS, new StatsEffect() },
                { EffectEnum.STAT_MAS_PM, new StatsEffect() },
                { EffectEnum.STAT_MAS_PM_BONUS, new StatsEffect() },
                { EffectEnum.STAT_MENOS_PA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_PM, new StatsEffect() },
                { EffectEnum.STAT_MENOS_PA_ESQUIVABLE, new APDodgeSubstractEffect() },
                { EffectEnum.STAT_MENOS_PM_ESQUIVABLE, new MPDodgeSubstractEffect() },
                { EffectEnum.STAT_MAS_ESQUIVA_PA, new StatsEffect() },
                { EffectEnum.STAT_MAS_ESQUIVA_PM, new StatsEffect() },
                { EffectEnum.STAT_MENOS_ESQUIVA_PA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_ESQUIVA_PM, new StatsEffect() },


                { EffectEnum.STAT_MAS_REDUCCION_DANO_FISICO, new StatsEffect() },
                { EffectEnum.STAT_MAS_REDUCCION_DANO_MAGICO, new StatsEffect() },
                { EffectEnum.STAT_MAS_ALCANCE, new StatsEffect() },
                { EffectEnum.STAT_MENOS_ALCANCE, new StatsEffect() },
                { EffectEnum.STAT_MAS_FUERZA, new StatsEffect() },
                { EffectEnum.STAT_MAS_INTELIGENCIA, new StatsEffect() },
                { EffectEnum.STAT_MAS_AGILIDAD, new StatsEffect() },
                { EffectEnum.STAT_MAS_SUERTE, new StatsEffect() },
                { EffectEnum.STAT_MAS_SABIDURIA, new StatsEffect() },
                { EffectEnum.STAT_MAS_VIDA, new StatsEffect() },
                { EffectEnum.STAT_MAS_VITALIDAD, new StatsEffect() },
                { EffectEnum.STAT_MENOS_FUERZA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_INTELIGENCIA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_AGILIDAD, new StatsEffect() },
                { EffectEnum.STAT_MENOS_SUERTE, new StatsEffect() },
                { EffectEnum.STAT_MENOS_SABIDURIA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_VITALIDAD, new StatsEffect() },
                { EffectEnum.STAT_MAS_INVOCACIONES_MAX, new StatsEffect() },
                { EffectEnum.STAT_MAS_PROSPECCION, new StatsEffect() },


                { EffectEnum.STAT_MAS_CURAS, new StatsEffect() },
                { EffectEnum.STAT_MENOS_CURAS, new StatsEffect() },


                { EffectEnum.STAT_MAS_RESISTENCIA_AIRE, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_AGUA, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_FUEGO, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_TIERRA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_AIRE, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_AGUA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_FUEGO, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_NEUTRAL, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_TIERRA, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, new StatsEffect() },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_AIRE, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_AGUA, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_FUEGO, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_NEUTRAL, new StatsEffect() },
                { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_TIERRA, new StatsEffect() },


                { EffectEnum.STAT_MAS_DANO, new StatsEffect() },
                { EffectEnum.STAT_MAS_DANO_FISICO, new StatsEffect() },
                { EffectEnum.STAT_MAS_DANO_MAGICO, new StatsEffect() },
                { EffectEnum.STAT_MAS_FALLO_CRITICO, new StatsEffect() },
                { EffectEnum.STAT_MAS_DANO_CRITICO, new StatsEffect() },
                { EffectEnum.STAT_MAS_DANO_PORCENTAJE, new StatsEffect() },
                { EffectEnum.STAT_MENOS_DANO_PORCENTAJE, new StatsEffect() },
                { EffectEnum.STAT_MENOS_DANO, new StatsEffect() },
                { EffectEnum.STAT_MENOS_DANO_CRITICO, new StatsEffect() },
                { EffectEnum.STAT_MENOS_DANO_MAGICO, new StatsEffect() },
                { EffectEnum.STAT_MENOS_DANO_FISICO, new StatsEffect() },
                { EffectEnum.STAT_MAS_DANO_DEVUELTO, new StatsEffect() },
                { EffectEnum.STAT_MAS_DANO_DEVUELTO_OBJETO, new StatsEffect() },


                { EffectEnum.CASTIGO_MAS, new PunishmentEffect() },
                { EffectEnum.CASTIGO_EROSION, new MaxLifeErosionEffect() },


                { EffectEnum.MOVIMIENTO_EMPUJAR, new PushEffect() },
                { EffectEnum.MOVIMIENTO_ATRAER, new PushEffect() },
                { EffectEnum.MOVIMIENTO_EMPUJAR_MIEDO, new PushFearEffect() },


                { EffectEnum.APARIENCIA_CAMBIAR, new SkinChangeEffect() },
                { EffectEnum.ESTADO_MAS, new StateAddEffect() },
                { EffectEnum.ESTADO_QUITAR, new StateRemoveEffect() },
                { EffectEnum.ESTADO_INVISIBILIDAD, new StateAddEffect() },


                { EffectEnum.STAT_ROBO_FUERZA, new StatsStealEffect() },
                { EffectEnum.STAT_ROBO_SABIDURIA, new StatsStealEffect() },
                { EffectEnum.STAT_ROBO_INTELIGENCIA, new StatsStealEffect() },
                { EffectEnum.STAT_ROBO_AGILIDAD, new StatsStealEffect() },
                { EffectEnum.STAT_ROBO_SUERTE, new StatsStealEffect() },
                { EffectEnum.STAT_ROBO_VITALIDAD, new StatsStealEffect() },
                { EffectEnum.STAT_ROBO_PA, new StatsStealEffect() },
                { EffectEnum.STAT_ROBO_PM, new StatsStealEffect() },
                { EffectEnum.STAT_ROBO_ALCANCE, new StatsStealEffect() },


                { EffectEnum.COMBATE_SUERTE_ECAFLIP, new EcaflipChanceEffect() },
                { EffectEnum.COMBATE_PERCEPCION, new PerceptionEffect() },
                { EffectEnum.COMBATE_PASAR_TURNO, new TurnPassEffect() },
                { EffectEnum.STAT_MULTIPLICAR_DANO, new MultiplyDamageEffect() },
                { EffectEnum.STAT_MAESTRIA, new MasteryEffect() },


                { EffectEnum.COMBATE_SACRIFICIO, new SacrificeEffect() },
                { EffectEnum.MOVIMIENTO_INTERCAMBIAR_POSICION, new TransposeEffect() },


                { EffectEnum.DEFENSA_EVASION, new DamageDodgeEffect() },


                { EffectEnum.HECHIZO_MAS_DANO, new IncreaseSpellJetEffect() },


                { EffectEnum.INVOCACION_CRIATURA, new SummoningEffect() },
                { EffectEnum.INVOCACION_DOBLE, new SummoningEffect() },
                { EffectEnum.INVOCACION_ESTATICA, new SummoningEffect(true) },


                { EffectEnum.BUFF_QUITAR_TODOS, new BuffRemoveEffect() },


                { EffectEnum.PANDA_CARGAR, new PandaCarrierEffect() },
                { EffectEnum.PANDA_LANZAR, new PandaLaunchEffect() },


                { EffectEnum.COMBATE_COLOCAR_GLIFO, new ActivableObjectEffect() },
                { EffectEnum.COMBATE_COLOCAR_TRAMPA, new ActivableObjectEffect() },
                { EffectEnum.LANZAR_HECHIZO, new SubSpellEffect() },
                { EffectEnum.NINGUN_EFECTO, new NullEffect() },
                { EffectEnum.CARACTERISTICA_MAS_FUERZA, new CharacteristicBoostEffect() },
                { EffectEnum.CARACTERISTICA_MAS_SUERTE, new CharacteristicBoostEffect() },
                { EffectEnum.CARACTERISTICA_MAS_AGILIDAD, new CharacteristicBoostEffect() },
                { EffectEnum.CARACTERISTICA_MAS_VITALIDAD, new CharacteristicBoostEffect() },
                { EffectEnum.CARACTERISTICA_MAS_INTELIGENCIA, new CharacteristicBoostEffect() },
                { EffectEnum.CARACTERISTICA_MAS_SABIDURIA, new CharacteristicBoostEffect() },
                { EffectEnum.COMBATE_COLOCAR_GLIFO_BIS, new ActivableObjectEffect() },


                // Efectos de pelea añadidos para paridad con StarLoco (2026-06-14).
                { EffectEnum.COMBATE_MATAR_OBJETIVO, new KillEffect() },
                { EffectEnum.CURACION_VIDA_DEVUELTA, new HealEffect() },
                { EffectEnum.DANO_VIDA_NEUTRAL_BIS, new DamageLifePercentEffect(EffectEnum.DANO_BRUTO) },
                { EffectEnum.STAT_MENOS_PROSPECCION, new StatsEffect() },
                { EffectEnum.KAMAS_ROBO, new KamasStealEffect() },
                { EffectEnum.STAT_MAS_DANO_BIS, new StatsEffect() },
                { EffectEnum.DANO_SIN_BOOST, new StatsEffect() },
                { EffectEnum.STAT_MENOS_DANO_FIJO, new StatsEffect() },
                { EffectEnum.CURACION_AL_ATACAR, new VampirismEffect() }
            };
        }

        public FightActionResultEnum TryApplyEffect(CastInfos castInfos)
        {
            if (!m_effects.TryGetValue(castInfos.EffectType, out AbstractSpellEffect value))
            {
                Logger.Debug($"EffectManager::TryApplyEffect efecto desconocido: {castInfos.EffectType}");
                return FightActionResultEnum.RESULT_NOTHING;
            }
            return value.ApplyEffect(castInfos);
        }
    }
}


