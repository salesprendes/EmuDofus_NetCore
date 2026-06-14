using Game.Map;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class DamageDodgeBuff : AbstractSpellBuff
    {
        public DamageDodgeBuff(CastInfos castInfos, AbstractFighter target)
    : base(castInfos, target, ActiveType.ACTIVE_ATTACKED_BEFORE_JET, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            if (!damageInfos.IsMelee || Pathfinding.GoalDistance(Target.Map, Target.Cell.Id, damageInfos.Caster.Cell.Id) > 1)
                return FightActionResultEnum.RESULT_NOTHING;

            damageValue = 0;


            if (Target.Cell.Id != damageInfos.TargetKnownCellId)
                return FightActionResultEnum.RESULT_NOTHING;

            var subInfos = new CastInfos(EffectEnum.MOVIMIENTO_EMPUJAR, 0, 0, 0, 0, 0, 0, 0, damageInfos.Caster, null);
            var direction = Pathfinding.GetDirection(Target.Fight.Map, damageInfos.Caster.Cell.Id, Target.Cell.Id);


            return PushEffect.ApplyPush(subInfos, Target, direction, 1);
        }
    }
}


