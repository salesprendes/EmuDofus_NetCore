using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class StatsStealEffect : AbstractSpellEffect
    {
        private static Dictionary<EffectEnum, EffectEnum> _targetMalus = new Dictionary<EffectEnum, EffectEnum>()
        {
            { EffectEnum.STAT_ROBO_FUERZA          , EffectEnum.STAT_MENOS_FUERZA         },
            { EffectEnum.STAT_ROBO_INTELIGENCIA       , EffectEnum.STAT_MENOS_INTELIGENCIA  },
            { EffectEnum.STAT_ROBO_AGILIDAD           , EffectEnum.STAT_MENOS_AGILIDAD       },
            { EffectEnum.STAT_ROBO_SABIDURIA            , EffectEnum.STAT_MENOS_SABIDURIA       },
            { EffectEnum.STAT_ROBO_SUERTE            , EffectEnum.STAT_MENOS_SUERTE        },
            { EffectEnum.STAT_ROBO_VITALIDAD          , EffectEnum.STAT_MENOS_VITALIDAD      },
            { EffectEnum.STAT_ROBO_PA                , EffectEnum.STAT_MENOS_PA            },
            { EffectEnum.STAT_ROBO_PM                , EffectEnum.STAT_MENOS_PM            },
            { EffectEnum.STAT_ROBO_ALCANCE                , EffectEnum.STAT_MENOS_ALCANCE            },
        };

        private static Dictionary<EffectEnum, EffectEnum> _casterBonus = new Dictionary<EffectEnum, EffectEnum>()
        {
            { EffectEnum.STAT_ROBO_FUERZA          , EffectEnum.STAT_MAS_FUERZA         },
            { EffectEnum.STAT_ROBO_INTELIGENCIA       , EffectEnum.STAT_MAS_INTELIGENCIA  },
            { EffectEnum.STAT_ROBO_AGILIDAD           , EffectEnum.STAT_MAS_AGILIDAD       },
            { EffectEnum.STAT_ROBO_SABIDURIA            , EffectEnum.STAT_MAS_SABIDURIA       },
            { EffectEnum.STAT_ROBO_SUERTE            , EffectEnum.STAT_MAS_SUERTE        },
            { EffectEnum.STAT_ROBO_VITALIDAD          , EffectEnum.STAT_MAS_VITALIDAD      },
            { EffectEnum.STAT_ROBO_PA                , EffectEnum.STAT_MAS_PA            },
            { EffectEnum.STAT_ROBO_PM                , EffectEnum.STAT_MAS_PM            },
            { EffectEnum.STAT_ROBO_ALCANCE                , EffectEnum.STAT_MAS_ALCANCE            },
        };

        public override FightActionResultEnum ApplyEffect(CastInfos CastInfos)
        {
            if (CastInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            if (!_targetMalus.TryGetValue(CastInfos.EffectType, out var malusType)
                || !_casterBonus.TryGetValue(CastInfos.EffectType, out var bonusType))
                return FightActionResultEnum.RESULT_NOTHING;

            var malusInfos = new CastInfos(malusType, CastInfos.SpellId, CastInfos.CellId, CastInfos.Value1, CastInfos.Value2, CastInfos.Value3, CastInfos.Chance, CastInfos.Duration, CastInfos.Caster, CastInfos.Target);
            var bonusInfos = new CastInfos(bonusType, CastInfos.SpellId, CastInfos.CellId, CastInfos.Value1, CastInfos.Value2, CastInfos.Value3, CastInfos.Chance, CastInfos.Duration - 1, CastInfos.Caster, CastInfos.Target);
            var damageValue = 0;

            if (CastInfos.Target == CastInfos.Caster)
                return FightActionResultEnum.RESULT_NOTHING;


            var BuffStats = new StatsBuff(malusInfos, CastInfos.Target);
            if (BuffStats.ApplyEffect(ref damageValue) == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            CastInfos.Target.BuffManager.AddBuff(BuffStats);


            BuffStats = new StatsBuff(bonusInfos, CastInfos.Caster);
            CastInfos.Caster.BuffManager.AddBuff(BuffStats);

            return BuffStats.ApplyEffect(ref damageValue);
        }
    }
}


