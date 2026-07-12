using Game.Spell;

namespace Game.Fight.Effect.Type
{
    public sealed class LifePercentOnAttackedBuff : AbstractSpellBuff
    {
        private readonly EffectEnum m_damageType;

        public LifePercentOnAttackedBuff(CastInfos castInfos, AbstractFighter target, EffectEnum damageType)
            : base(castInfos, target, ActiveType.ACTIVE_ATTACKED_AFTER_JET, DecrementType.TYPE_ENDTURN)
        {
            m_damageType = damageType;
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            var self = (int)((long)Target.Life * CastInfos.Value1 / 100);
            if (self <= 0)
                return base.ApplyEffect(ref damageValue, damageInfos);

            var selfInfos = new CastInfos(m_damageType, CastInfos.SpellId, -1, 0, 0, 0, 0, 0, CastInfos.Caster, Target);
            selfInfos.IsPoison = true;

            return DamageEffect.ApplyDamages(selfInfos, Target, ref self);
        }
    }
}
