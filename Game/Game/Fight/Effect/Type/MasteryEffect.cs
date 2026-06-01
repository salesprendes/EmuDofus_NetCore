namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// Weapon mastery buffs are self-targeted even when the client does not provide a target entity.
    /// </summary>
    public sealed class MasteryEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            var target = castInfos.Target ?? castInfos.Caster;
            if (target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            target.BuffManager.AddBuff(new MasteryBuff(castInfos, target));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


