using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class TransposeEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            if (castInfos.SpellId == 445)
            {
                if (castInfos.Target.Team == castInfos.Caster.Team)
                    return FightActionResultEnum.RESULT_NOTHING;
            }
            else if (castInfos.SpellId == 438)
            {
                if (castInfos.Target.Team != castInfos.Caster.Team)
                    return FightActionResultEnum.RESULT_NOTHING;
            }

            var targetTeleport = new CastInfos(EffectEnum.MOVIMIENTO_TELETRANSPORTAR, castInfos.SpellId, castInfos.Caster.Cell.Id, 0, 0, 0, 0, 0, castInfos.Target, null);
            var casterTeleport = new CastInfos(EffectEnum.MOVIMIENTO_TELETRANSPORTAR, castInfos.SpellId, castInfos.Target.Cell.Id, 0, 0, 0, 0, 0, castInfos.Caster, null);

            castInfos.Caster.SetCell(null);
            castInfos.Target.SetCell(null);

            if (TeleportEffect.ApplyTeleport(targetTeleport) == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            if (TeleportEffect.ApplyTeleport(casterTeleport) == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


