using Game.Spell;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect
{
    public enum ActiveType
    {
        ACTIVE_STATS,
        ACTIVE_BEGINTURN,
        ACTIVE_ENDTURN,
        ACTIVE_ENDMOVE,
        ACTIVE_ATTACK_BEFORE_JET,
        ACTIVE_ATTACK_AFTER_JET,
        ACTIVE_ATTACKED_BEFORE_JET,
        ACTIVE_ATTACKED_AFTER_JET,
    }

    public enum DecrementType
    {
        TYPE_BEGINTURN,
        TYPE_ENDTURN,
        TYPE_ENDMOVE,
    }

    public abstract class AbstractSpellBuff
    {
        public DecrementType DecrementType
        {
            get;
            set;
        }

        public ActiveType ActiveType
        {
            get;
            set;
        }


        public CastInfos CastInfos
        {
            get;
            set;
        }

        public AbstractFighter Caster
        {
            get;
            set;
        }

        public AbstractFighter Target
        {
            get;
            set;
        }

        public int Duration
        {
            get;
            set;
        }

        public bool IsDebuffable
        {
            get
            {
                switch (CastInfos.SubEffect)
                {
                    case EffectEnum.AddState:
                    case EffectEnum.ChangeSkin:
                    case EffectEnum.AddChatiment:
                        return false;
                }

                return true;
            }
        }

        public AbstractSpellBuff(CastInfos castInfos, AbstractFighter target, ActiveType activeType, DecrementType decrementType)
        {
            CastInfos = castInfos;
            Duration = target.Fight.CurrentFighter == target ? castInfos.Duration + 1 : castInfos.Duration;
            Caster = castInfos.Caster;
            Target = target;

            ActiveType = activeType;
            DecrementType = decrementType;

            switch (castInfos.EffectType)
            {
                case EffectEnum.ReflectSpell:
                    Target.Fight.Dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType,
                                                                               Target.Id,
                                                                               CastInfos.Value2.ToString(),
                                                                               CastInfos.Value2.ToString(),
                                                                               "10",
                                                                               CastInfos.Value2.ToString(),
                                                                               CastInfos.Duration.ToString(),
                                                                               CastInfos.SpellId.ToString()));
                    break;

                case EffectEnum.EcaflipChance:
                case EffectEnum.AddChatiment:
                    Target.Fight.Dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType,
                                                                           Target.Id,
                                                                           CastInfos.Value1.ToString(),
                                                                           CastInfos.Value2.ToString(),
                                                                           CastInfos.Value3.ToString(),
                                                                           "",
                                                                           CastInfos.Duration.ToString(),
                                                                           CastInfos.SpellId.ToString()));
                    break;

                case EffectEnum.PandaCarrier:
                    break;

                default:
                    Target.Fight.Dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType,
                                                                               Target.Id,
                                                                               CastInfos.Value1.ToString(),
                                                                               "",
                                                                               "",
                                                                               "",
                                                                               CastInfos.Duration.ToString(),
                                                                               CastInfos.SpellId.ToString()));
                    break;
            }
        }

        public void SendTo(Action<string> dispatch)
        {
            switch (CastInfos.EffectType)
            {
                case EffectEnum.ReflectSpell:
                    dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType,
                                                                   Target.Id,
                                                                   CastInfos.Value2.ToString(),
                                                                   CastInfos.Value2.ToString(),
                                                                   "10",
                                                                   CastInfos.Value2.ToString(),
                                                                   Duration.ToString(),
                                                                   CastInfos.SpellId.ToString()));
                    break;

                case EffectEnum.EcaflipChance:
                case EffectEnum.AddChatiment:
                    dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType,
                                                               Target.Id,
                                                               CastInfos.Value1.ToString(),
                                                               CastInfos.Value2.ToString(),
                                                               CastInfos.Value3.ToString(),
                                                               "",
                                                               Duration.ToString(),
                                                               CastInfos.SpellId.ToString()));
                    break;

                case EffectEnum.PandaCarrier:
                    break;

                default:
                    dispatch(WorldMessage.FIGHT_EFFECT_INFORMATION(CastInfos.EffectType, Target.Id, CastInfos.Value1.ToString(), "", "", "", Duration.ToString(), CastInfos.SpellId.ToString()));
                    break;
            }
        }

        public virtual FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            return Caster.Fight.TryKillFighter(Target, Caster);
        }

        public virtual FightActionResultEnum RemoveEffect()
        {
            return Caster.Fight.TryKillFighter(Target, Caster);
        }

        public int DecrementDuration()
        {
            Duration--;

            CastInfos.FakeValue = 0;

            return Duration;
        }
    }
}


