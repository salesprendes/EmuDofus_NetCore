using Game.Action;
using Game.Fight.AI;
using Game.Fight.AI.Core;
using Game.Spell;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class DamageEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;


            if (castInfos.Duration > 0)
            {

                castInfos.IsPoison = true;

                castInfos.Target.BuffManager.AddBuff(new DamageBuff(castInfos, castInfos.Target));
            }
            else
            {
                var damageValue = castInfos.RandomJet;

                return DamageEffect.ApplyDamages(castInfos, castInfos.Target, ref damageValue);
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public static FightActionResultEnum ApplyDamages(CastInfos castInfos, AbstractFighter target, ref int damageJet)
        {
            var caster = castInfos.Caster;


            if (!castInfos.IsPoison && !castInfos.IsTrap && !castInfos.IsReflect && caster.StateManager.HasState(FighterStateEnum.STATE_STEALTH))
                caster.BuffManager.RemoveStealth();


            if (!castInfos.IsPoison && !castInfos.IsReflect)
            {
                if (caster.BuffManager.OnAttackBeforeJet(castInfos, ref damageJet) == FightActionResultEnum.RESULT_END)
                    return FightActionResultEnum.RESULT_END;

                if (target.BuffManager.OnAttackedBeforeJet(castInfos, ref damageJet) == FightActionResultEnum.RESULT_END)
                    return FightActionResultEnum.RESULT_END;
            }


            caster.CalculDamages(castInfos.EffectType, ref damageJet);


            if (!castInfos.IsPoison && !castInfos.IsReflect && !castInfos.IsTrap && damageJet > 0 && target is AIFighter aiTarget && aiTarget.CurrentBrain is IDamageReceivedBrain damageReceiver)
            {
                damageReceiver.OnDamageReceived(castInfos, damageJet);
            }


            target.CalculReduceDamages(castInfos.EffectType, ref damageJet);


            if (damageJet > 0)
            {

                if (!castInfos.IsPoison && !castInfos.IsReflect)
                {

                    var armor = target.CalculArmor(castInfos.EffectType);


                    if (armor != 0)
                    {
                        castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_ARMOR, target.Id, target.Id + "," + armor));

                        damageJet -= armor;

                        if (damageJet < 0)
                            damageJet = 0;
                    }
                }
            }


            if (!castInfos.IsPoison && !castInfos.IsReflect)
            {
                if (caster.BuffManager.OnAttackAfterJet(castInfos, ref damageJet) == FightActionResultEnum.RESULT_END)
                    return FightActionResultEnum.RESULT_END;
                if (target.BuffManager.OnAttackedAfterJet(castInfos, ref damageJet) == FightActionResultEnum.RESULT_END)
                    return FightActionResultEnum.RESULT_END;
            }


            if (damageJet > 0)
            {

                if (!castInfos.IsPoison && !castInfos.IsReflect && castInfos.EffectType != EffectEnum.DANO_BRUTO)
                {
                    var reflectDamage = target.ReflectDamage;


                    if (reflectDamage > 0)
                    {
                        target.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.STAT_MAS_DANO_DEVUELTO, target.Id, target.Id + "," + reflectDamage));


                        if (reflectDamage > damageJet)
                            reflectDamage = damageJet;

                        var subInfos = new CastInfos(castInfos.EffectType, 0, 0, 0, 0, 0, 0, 0, target, null);
                        subInfos.IsReflect = true;


                        if (DamageEffect.ApplyDamages(subInfos, caster, ref reflectDamage) == FightActionResultEnum.RESULT_END)
                            return FightActionResultEnum.RESULT_END;


                        damageJet -= reflectDamage;
                    }
                }
            }


            if (damageJet < 0)
                damageJet = 0;


            if (damageJet > target.Life)
                damageJet = target.Life;


            target.Life -= damageJet;


            castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_DAMAGE, caster.Id, target.Id + ",-" + damageJet));


            return castInfos.Fight.TryKillFighter(target, caster);
        }
    }
}
