using Game.Network;
using Game.Spell;

namespace Game.Fight.Effect.Type
{
    public sealed class TurnPassBuff : AbstractSpellBuff
    {
        public TurnPassBuff(CastInfos castInfos, AbstractFighter target)
            : base(castInfos, target, ActiveType.ACTIVE_BEGINTURN, DecrementType.TYPE_BEGINTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            Target.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.COMBATE_PASAR_TURNO, Caster.Id, Target.Id.ToString()));
            Target.TurnPass = true;

            return base.ApplyEffect(ref damageValue, damageInfos);
        }
    }
}


