using Game.Network;
using Game.Spell;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// Forces the target to immediately skip its turn when the buff triggers at begin turn.
    /// </summary>
    public sealed class TurnPassBuff : AbstractSpellBuff
    {
        public TurnPassBuff(CastInfos castInfos, AbstractFighter target)
            : base(castInfos, target, ActiveType.ACTIVE_BEGINTURN, DecrementType.TYPE_BEGINTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            Target.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.TurnPass, Caster.Id, Target.Id.ToString()));
            Target.TurnPass = true;

            return base.ApplyEffect(ref damageValue, damageInfos);
        }
    }
}


