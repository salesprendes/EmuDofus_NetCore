namespace Game.Fight.Effect.Type
{
    public sealed class StatsEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            var subInfos = new CastInfos(castInfos.EffectType, castInfos.SpellId, castInfos.CellId, castInfos.RandomJet, castInfos.Value2, castInfos.Value3, castInfos.Chance, castInfos.Duration, castInfos.Caster, castInfos.Target);
            var buffStats = new StatsBuff(subInfos, castInfos.Target);
            var damageValue = 0;
            if (buffStats.ApplyEffect(ref damageValue) == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            castInfos.Target.BuffManager.AddBuff(buffStats);

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


