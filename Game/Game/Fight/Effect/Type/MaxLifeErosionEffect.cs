using Game.Entity;
using Game.Network;
using Game.Spell;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// E776 "pierde #1% de los PDV máximos": la contrapartida de los castigos del Sacrieur
    /// (y la erosión de hechizos como Gas Mortal). Reduce la vida máxima el porcentaje indicado
    /// mientras dura, recortando la vida actual si queda por encima del nuevo máximo.
    /// </summary>
    public sealed class MaxLifeErosionEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null || castInfos.Value1 <= 0)
                return FightActionResultEnum.RESULT_NOTHING;

            var amount = castInfos.Target.MaxLife * castInfos.Value1 / 100;
            if (amount <= 0)
                return FightActionResultEnum.RESULT_NOTHING;

            var buff = new MaxLifeErosionBuff(castInfos, castInfos.Target, amount);
            buff.ApplyErosion();
            castInfos.Target.BuffManager.AddBuff(buff);

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }

    public sealed class MaxLifeErosionBuff : AbstractSpellBuff
    {
        private readonly int m_amount;

        public MaxLifeErosionBuff(CastInfos castInfos, AbstractFighter target, int amount)
            : base(castInfos, target, ActiveType.ACTIVE_STATS, DecrementType.TYPE_ENDTURN)
        {
            m_amount = amount;
        }

        public void ApplyErosion()
        {
            Target.Statistics.AddBoosts(EffectEnum.STAT_MAS_VIDA, -m_amount);
            Target.Statistics.StatisticsChanged();

            if (Target.Life > Target.MaxLife)
                Target.Life = Target.MaxLife;

            if (Target is CharacterEntity character)
                character.Dispatch(WorldMessage.ACCOUNT_STATS(character));
        }

        public override FightActionResultEnum RemoveEffect()
        {
            // Restaura la vida máxima; la vida actual no se regenera (solo sube el techo).
            Target.Statistics.AddBoosts(EffectEnum.STAT_MAS_VIDA, m_amount);
            Target.Statistics.StatisticsChanged();

            if (Target is CharacterEntity character)
                character.Dispatch(WorldMessage.ACCOUNT_STATS(character));

            return base.RemoveEffect();
        }
    }
}
