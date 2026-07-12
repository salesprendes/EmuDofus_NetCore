using Game.Action;
using Game.Map;
using Game.Spell;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class PushFearEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Caster?.Cell == null || castInfos.Caster.Cell.Id == castInfos.CellId)
                return FightActionResultEnum.RESULT_NOTHING;

            // Cardinal siempre: con una celda de lanzamiento no alineada, la 8-direcciones
            // devolvía una diagonal y el efecto buscaba al objetivo en una celda inválida.
            DirectionEnum direction = Pathfinding.InLine(castInfos.Map, castInfos.Caster.Cell.Id, castInfos.CellId)
                ? Pathfinding.GetDirection(castInfos.Map, castInfos.Caster.Cell.Id, castInfos.CellId)
                : Pathfinding.GetCardinalDirection(castInfos.Map, castInfos.Caster.Cell.Id, castInfos.CellId);
            var targetFighterCell = Pathfinding.NextCell(castInfos.Map, castInfos.Caster.Cell.Id, direction);

            var target = castInfos.Fight.GetFighterOnCell(targetFighterCell);
            if (target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            var distance = Pathfinding.GoalDistance(castInfos.Map, target.Cell.Id, castInfos.CellId);
            var currentCell = target.Cell;

            for (int i = 0; i < distance; i++)
            {
                // Paso validado: en el borde del mapa el huido choca en vez de reaparecer en el
                // extremo opuesto (wrap-around).
                var nextCell = Pathfinding.TryGetCellInDirection(castInfos.Map, currentCell.Id, direction, 1, out var nextCellId)
                    ? castInfos.Fight.GetCell(nextCellId)
                    : null;

                if (nextCell != null && nextCell.CanWalk)
                {
                    if (nextCell.HasObject(FightObstacleTypeEnum.TYPE_TRAP))
                    {
                        castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_PUSHBACK, target.Id, target.Id + "," + nextCell.Id));

                        castInfos.Fight.SetSubAction(() => { return target.SetCell(nextCell); }, 1 + ++i * WorldConfig.FIGHT_PUSH_CELL_TIME);

                        return FightActionResultEnum.RESULT_NOTHING;
                    }
                }
                else
                {
                    if (i != 0)
                    {
                        castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_PUSHBACK, target.Id, target.Id + "," + currentCell.Id));
                    }

                    castInfos.Fight.SetSubAction(() => { return target.SetCell(currentCell); }, 1 + (i * WorldConfig.FIGHT_PUSH_CELL_TIME));

                    return FightActionResultEnum.RESULT_NOTHING;
                }

                currentCell = nextCell;
            }

            castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_PUSHBACK, target.Id, target.Id + "," + currentCell.Id));

            castInfos.Fight.SetSubAction(() => { return target.SetCell(currentCell); }, 1 + distance * WorldConfig.FIGHT_PUSH_CELL_TIME);

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


