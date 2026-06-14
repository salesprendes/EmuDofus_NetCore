using Game.Entity;
using Game.Network;
using Game.Spell;

namespace Game.Fight.Effect.Type
{
    public sealed class StatsBuff : AbstractSpellBuff
    {
        public StatsBuff(CastInfos castInfos, AbstractFighter target) : base(castInfos, target, ActiveType.ACTIVE_STATS, DecrementType.TYPE_ENDTURN) { }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            int showValue;

            switch (CastInfos.EffectType)
            {
                case EffectEnum.STAT_MENOS_PA:
                case EffectEnum.STAT_MENOS_PM:
                case EffectEnum.STAT_MENOS_PA_ESQUIVABLE:
                case EffectEnum.STAT_MENOS_PM_ESQUIVABLE:
                    showValue = -CastInfos.Value1;
                    break;

                default:
                    showValue = CastInfos.Value1;
                    break;
            }

            if (CastInfos.EffectType != EffectEnum.DEFENSA_DEVOLVER_HECHIZO)
                Target.Fight.Dispatch(WorldMessage.GAME_ACTION(CastInfos.EffectType, Target.Id, Target.Id + "," + showValue + "," + Duration));

            Target.Statistics.AddBoosts(CastInfos.EffectType, CastInfos.Value1);
            Target.Statistics.StatisticsChanged();

            if (Target is CharacterEntity characterApply)
                characterApply.Dispatch(WorldMessage.ACCOUNT_STATS(characterApply));

            return base.ApplyEffect(ref damageValue, damageInfos);
        }

        public override FightActionResultEnum RemoveEffect()
        {
            Target.Statistics.GetEffect(CastInfos.EffectType).Boosts -= CastInfos.Value1;
            Target.Statistics.StatisticsChanged();

            if (Target is CharacterEntity characterRemove)
                characterRemove.Dispatch(WorldMessage.ACCOUNT_STATS(characterRemove));

            if (Target.Fight != null)
            {
                switch (CastInfos.EffectType)
                {
                    case EffectEnum.STAT_MENOS_PA:
                    case EffectEnum.STAT_MENOS_PA_ESQUIVABLE:
                    case EffectEnum.STAT_MENOS_PM:
                    case EffectEnum.STAT_MENOS_PM_ESQUIVABLE:
                        Target.Fight.Dispatch(WorldMessage.GAME_ACTION(CastInfos.EffectType, Target.Id, Target.Id + "," + CastInfos.Value1 + "," + 1));
                        break;

                    case EffectEnum.STAT_MAS_PA:
                    case EffectEnum.STAT_MAS_PA_BIS:
                    case EffectEnum.STAT_MAS_PM:
                    case EffectEnum.STAT_MAS_PM_BONUS:
                        Target.Fight.Dispatch(WorldMessage.GAME_ACTION(CastInfos.EffectType, Target.Id, Target.Id + "," + (-CastInfos.Value1) + "," + 1));
                        break;
                }
            }

            return base.RemoveEffect();
        }
    }
}


