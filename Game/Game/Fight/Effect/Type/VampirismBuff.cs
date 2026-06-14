using Game.Action;
using Game.Network;
using System;

namespace Game.Fight.Effect.Type
{
    public sealed class VampirismBuff : AbstractSpellBuff
    {
        public VampirismBuff(CastInfos castInfos, AbstractFighter target) : base(castInfos, target, ActiveType.ACTIVE_ATTACK_AFTER_JET, DecrementType.TYPE_ENDTURN) {}

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            if (damageValue > 0 && Target.Life < Target.MaxLife)
            {
                var heal = damageValue * Math.Max(1, CastInfos.Value1) / 100;
                if (heal > 0)
                {
                    if (Target.Life + heal > Target.MaxLife)
                        heal = Target.MaxLife - Target.Life;

                    Target.Life += heal;
                    Target.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_HEAL, Target.Id, Target.Id + "," + heal));
                }
            }

            return base.ApplyEffect(ref damageValue, damageInfos);
        }
    }
}
