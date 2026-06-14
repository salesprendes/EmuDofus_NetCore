using Game.Network;
using Game.Spell;

namespace Game.Fight.Effect.Type
{
    public sealed class SkinChangeEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            if (castInfos.Duration > 0)
            {
                castInfos.Target.BuffManager.AddBuff(new SkinChangeBuff(castInfos, castInfos.Target));
            }
            else
            {
                return ApplySkinChange(castInfos);
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public static FightActionResultEnum ApplySkinChange(CastInfos castInfos)
        {
            if (castInfos.Value1 <= 0 && castInfos.Value2 <= 0 && castInfos.Value3 <= 0)
            {
                castInfos.Target.BuffManager.RemoveSkin();
            }
            else
            {
                var currentSkin = castInfos.Target.Skin;
                var newSkin = castInfos.Value3 == -1 ? currentSkin : castInfos.Value3;

                castInfos.Value3 = castInfos.Target.Skin;
                castInfos.Target.Skin = newSkin;

                castInfos.Target.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.APARIENCIA_CAMBIAR, castInfos.Caster.Id, castInfos.Target.Id + "," + currentSkin + "," + newSkin + "," + (castInfos.Duration + 1)));
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}
