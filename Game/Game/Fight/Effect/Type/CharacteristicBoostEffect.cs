using Game.Spell;
using System.Collections.Generic;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// E607-E611/E678 "+X a la característica": los usan hechizos de monstruo (Rabia Sanguinaria,
    /// Luz Nocturna...). En combate equivalen a un boost temporal de la stat correspondiente, así
    /// que se traducen a su STAT_MAS_* y se aplican como StatsBuff normal.
    /// </summary>
    public sealed class CharacteristicBoostEffect : AbstractSpellEffect
    {
        private static readonly Dictionary<EffectEnum, EffectEnum> StatByCharacteristic = new Dictionary<EffectEnum, EffectEnum>
        {
            { EffectEnum.CARACTERISTICA_MAS_FUERZA, EffectEnum.STAT_MAS_FUERZA },
            { EffectEnum.CARACTERISTICA_MAS_SUERTE, EffectEnum.STAT_MAS_SUERTE },
            { EffectEnum.CARACTERISTICA_MAS_AGILIDAD, EffectEnum.STAT_MAS_AGILIDAD },
            { EffectEnum.CARACTERISTICA_MAS_VITALIDAD, EffectEnum.STAT_MAS_VITALIDAD },
            { EffectEnum.CARACTERISTICA_MAS_INTELIGENCIA, EffectEnum.STAT_MAS_INTELIGENCIA },
            { EffectEnum.CARACTERISTICA_MAS_SABIDURIA, EffectEnum.STAT_MAS_SABIDURIA },
        };

        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null || !StatByCharacteristic.TryGetValue(castInfos.EffectType, out var statType))
                return FightActionResultEnum.RESULT_NOTHING;

            var subInfos = new CastInfos(statType, castInfos.SpellId, castInfos.CellId, castInfos.RandomJet, castInfos.Value2, castInfos.Value3, castInfos.Chance, castInfos.Duration, castInfos.Caster, castInfos.Target);
            var buff = new StatsBuff(subInfos, castInfos.Target);
            var damageValue = 0;
            if (buff.ApplyEffect(ref damageValue) == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            castInfos.Target.BuffManager.AddBuff(buff);

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}
