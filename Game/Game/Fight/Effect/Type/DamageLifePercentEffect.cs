using Game.Spell;

namespace Game.Fight.Effect.Type
{
    public sealed class DamageLifePercentEffect : AbstractSpellEffect
    {
        private EffectEnum m_damageType;

        public DamageLifePercentEffect(EffectEnum damageType)
        {
            m_damageType = damageType;
        }

        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null || castInfos.Caster == null)
                return FightActionResultEnum.RESULT_NOTHING;

            // E89 con duracion y sobre el propio lanzador (mascara "solo lanzador", como Furia):
            // NO es dano instantaneo, sino un BUFF reactivo: cada vez que el portador es atacado,
            // sufre Value1% de su vida ACTUAL como dano (la contrapartida "berserker" de Furia).
            if (castInfos.Duration > 0 && castInfos.Target == castInfos.Caster)
            {
                castInfos.Target.BuffManager.AddBuff(new LifePercentOnAttackedBuff(castInfos, castInfos.Target, m_damageType));
                return FightActionResultEnum.RESULT_NOTHING;
            }

            // E85-89/671 (dano instantaneo): el dano es un % de la vida del ATACANTE, aplicado a
            // cualquier objetivo. Division sin perder precision.
            var damageInfos = new CastInfos(m_damageType, -1, -1, -1, -1, -1, -1, -1, castInfos.Caster, castInfos.Target);
            var damageJet = (int)((long)castInfos.Caster.Life * castInfos.RandomJet / 100);

            return DamageEffect.ApplyDamages(damageInfos, damageInfos.Target, ref damageJet);
        }
    }
}


