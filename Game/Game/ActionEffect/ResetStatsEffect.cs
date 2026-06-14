using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class ResetStatsEffect : AbstractActionEffect<ResetStatsEffect>
    {
        public override bool ProcessItem(Entity.CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, null);
        }

        public override bool Process(Entity.CharacterEntity character, Dictionary<string, string> parameters)
        {
            character.CachedBuffer = true;
            character.CaractPoint = (character.Level - 1) * 5;
            character.Statistics.AddBase(EffectEnum.STAT_MAS_VITALIDAD, -character.DatabaseRecord.Vitality);
            character.Statistics.AddBase(EffectEnum.STAT_MAS_SABIDURIA, -character.DatabaseRecord.Wisdom);
            character.Statistics.AddBase(EffectEnum.STAT_MAS_INTELIGENCIA, -character.DatabaseRecord.Intelligence);
            character.Statistics.AddBase(EffectEnum.STAT_MAS_FUERZA, -character.DatabaseRecord.Strength);
            character.Statistics.AddBase(EffectEnum.STAT_MAS_AGILIDAD, -character.DatabaseRecord.Agility);
            character.Statistics.AddBase(EffectEnum.STAT_MAS_SUERTE, -character.DatabaseRecord.Chance);
            character.DatabaseRecord.Vitality = 0;
            character.DatabaseRecord.Wisdom = 0;
            character.DatabaseRecord.Intelligence = 0;
            character.DatabaseRecord.Strength = 0;
            character.DatabaseRecord.Agility = 0;
            character.DatabaseRecord.Chance = 0;
            character.SendAccountStats();
            character.CachedBuffer = false;

            return true;
        }
    }
}


