using Game.Action;
using Game.Spell;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class HealEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;


            if (castInfos.Duration > 0)
            {
                castInfos.Target.BuffManager.AddBuff(new HealBuff(castInfos, castInfos.Target));
            }
            else
            {
                var healValue = castInfos.RandomJet;
                return HealEffect.ApplyHeal(castInfos, castInfos.Target, ref healValue);
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public static FightActionResultEnum ApplyHeal(CastInfos castInfos, AbstractFighter target, ref int heal)
        {
            var caster = castInfos.Caster;

            if (castInfos.EffectType != EffectEnum.DamageBrut)
                caster.CalculHeal(ref heal);

            if (target.Life + heal > target.MaxLife)
                heal = target.MaxLife - target.Life;

            target.Life += heal;

            castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_HEAL, caster.Id, target.Id + "," + heal));

            return castInfos.Fight.TryKillFighter(target, caster);
        }
    }
}


