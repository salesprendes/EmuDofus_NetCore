using System;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// Multiplies the direct damage of the owner's next attacks for the buff duration.
    /// </summary>
    public sealed class MultiplyDamageBuff : AbstractSpellBuff
    {
        public MultiplyDamageBuff(CastInfos castInfos, AbstractFighter target)
            : base(castInfos, target, ActiveType.ACTIVE_ATTACK_BEFORE_JET, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            var multiplier = Math.Max(1, CastInfos.Value1);
            damageValue *= multiplier;

            return base.ApplyEffect(ref damageValue, damageInfos);
        }
    }
}


