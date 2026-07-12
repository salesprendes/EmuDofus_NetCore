using Game.Action;
using Game.Network;
using Game.Spell;

namespace Game.Fight.Effect.Type
{
    public sealed class TransposeEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null || castInfos.Caster == null)
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

            var casterCell = castInfos.Caster.Cell;
            var targetCell = castInfos.Target.Cell;

            // Validar ANTES de vaciar celdas: la transposición es un intercambio directo, no un
            // teletransporte. Con el flujo anterior (dos ApplyTeleport, que abortan si el luchador
            // está enraizado/gravedad/muerto) uno podía quedarse con Cell == null para siempre
            // y lanzar NRE cada inicio de turno.
            if (casterCell == null || targetCell == null || castInfos.Caster.IsFighterDead || castInfos.Target.IsFighterDead)
                return FightActionResultEnum.RESULT_NOTHING;

            castInfos.Caster.SetCell(null);
            castInfos.Target.SetCell(null);

            castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_TELEPORT, castInfos.Caster.Id, castInfos.Caster.Id + "," + targetCell.Id));
            castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_TELEPORT, castInfos.Target.Id, castInfos.Target.Id + "," + casterCell.Id));

            if (castInfos.Caster.SetCell(targetCell) == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;

            return castInfos.Target.SetCell(casterCell);
        }
    }
}


