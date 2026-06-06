using Game.Network;
using Game.Spell;

namespace Game.Fight.Effect.Type
{
    public sealed class SkinChangeBuff : AbstractSpellBuff
    {
        private bool m_effectRemoved = false;
        private readonly int m_originalSkin;

        public SkinChangeBuff(CastInfos castInfos, AbstractFighter target) : base(castInfos, target, ActiveType.ACTIVE_STATS, DecrementType.TYPE_ENDTURN)
        {
            m_originalSkin = target.Skin;
            var damageValue = 0;
            ApplyEffect(ref damageValue);
        }

        public override FightActionResultEnum ApplyEffect(ref int DamageValue, CastInfos DamageInfos = null)
        {
            var newSkin = CastInfos.Value3 == -1 ? Target.Skin : CastInfos.Value3;
            Target.Skin = newSkin;

            Target.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.ChangeSkin, CastInfos.Caster.Id, Target.Id + "," + m_originalSkin + "," + newSkin + "," + (CastInfos.Duration + 1)));

            return base.ApplyEffect(ref DamageValue, DamageInfos);
        }

        public override FightActionResultEnum RemoveEffect()
        {
            if (m_effectRemoved)
                return base.RemoveEffect();

            m_effectRemoved = true;
            Duration = 0;

            Target.Skin = m_originalSkin;
            
            if (Target.Fight != null)
            {
                Target.Fight.Dispatch("GIe" + Target.Id);
                Target.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.ChangeSkin, CastInfos.Caster.Id, Target.Id + "," + m_originalSkin + "," + m_originalSkin + "," + 1));
            }

            return base.RemoveEffect();
        }
    }
}
