using Game.Database.Structure;
using Game.Entity;
using Game.Spell;
using Game.Stats;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class AddStatsEffect : AbstractActionEffect<AddStatsEffect>
    {
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, new Dictionary<string, string>() { { "statsId", effect.Id.ToString() }, { "value", effect.RandomJet.ToString() } });
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            var addEffect = EffectEnum.NINGUNO;
            var effectType = (EffectEnum)int.Parse(parameters["statsId"]);
            var value = int.Parse(parameters["value"]);

            switch (effectType)
            {
                case EffectEnum.STAT_MAS_VITALIDAD:
                case EffectEnum.CARACTERISTICA_MAS_VITALIDAD:
                    addEffect = EffectEnum.STAT_MAS_VITALIDAD;
                    character.DatabaseRecord.Vitality += value;
                    break;

                case EffectEnum.STAT_MAS_SABIDURIA:
                case EffectEnum.CARACTERISTICA_MAS_SABIDURIA:
                    addEffect = EffectEnum.STAT_MAS_SABIDURIA;
                    character.DatabaseRecord.Wisdom += value;
                    break;

                case EffectEnum.STAT_MAS_INTELIGENCIA:
                case EffectEnum.CARACTERISTICA_MAS_INTELIGENCIA:
                    addEffect = EffectEnum.STAT_MAS_INTELIGENCIA;
                    character.DatabaseRecord.Intelligence += value;
                    break;

                case EffectEnum.STAT_MAS_FUERZA:
                case EffectEnum.CARACTERISTICA_MAS_FUERZA:
                    addEffect = EffectEnum.STAT_MAS_FUERZA;
                    character.DatabaseRecord.Strength += value;
                    break;

                case EffectEnum.STAT_MAS_AGILIDAD:
                case EffectEnum.CARACTERISTICA_MAS_AGILIDAD:
                    addEffect = EffectEnum.STAT_MAS_AGILIDAD;
                    character.DatabaseRecord.Agility += value;
                    break;

                case EffectEnum.STAT_MAS_SUERTE:
                case EffectEnum.CARACTERISTICA_MAS_SUERTE:
                    addEffect = EffectEnum.STAT_MAS_SUERTE;
                    character.DatabaseRecord.Chance += value;
                    break;
            }

            character.Statistics.AddBase(addEffect, value);

            character.CachedBuffer = true;
            character.SendAccountStats();
            character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_CARACTERISTIC_UPGRADED, value));
            character.CachedBuffer = false;

            return true;
        }
    }
}


