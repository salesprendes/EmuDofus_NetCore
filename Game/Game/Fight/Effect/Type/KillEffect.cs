namespace Game.Fight.Effect.Type
{
    public sealed class KillEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            return castInfos.Fight.TryKillFighter(castInfos.Target, castInfos.Caster, force: true);
        }
    }
}
