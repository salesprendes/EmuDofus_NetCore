namespace Game.Fight.Effect.Type
{
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


