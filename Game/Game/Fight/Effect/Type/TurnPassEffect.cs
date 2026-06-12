namespace Game.Fight.Effect.Type
{
    public sealed class TurnPassEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            castInfos.Target.BuffManager.AddBuff(new TurnPassBuff(castInfos, castInfos.Target));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


