namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// Some buffs like Roue de la Fortune are encoded as a direct damage multiplier.
    /// </summary>
    public sealed class MultiplyDamageEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            var target = castInfos.Target ?? castInfos.Caster;
            if (target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            target.BuffManager.AddBuff(new MultiplyDamageBuff(castInfos, target));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


