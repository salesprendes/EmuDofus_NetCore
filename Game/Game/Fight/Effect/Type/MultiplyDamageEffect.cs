namespace Game.Fight.Effect.Type
{
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


