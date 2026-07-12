using Game.Action;
using Game.Entity;
using Game.Map;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Network;

namespace Game.Fight.Effect.Type
{
    public sealed class PushEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            if (castInfos.Target.Cell.Id != castInfos.TargetKnownCellId)
                return FightActionResultEnum.RESULT_NOTHING;

            DirectionEnum direction = DirectionEnum.Noreste;

            switch (castInfos.EffectType)
            {
                case EffectEnum.MOVIMIENTO_EMPUJAR:
                    // Origen del empuje = celda de lanzamiento (para las trampas, su centro).
                    // Como el cliente (getDirectionFromCoordinates con bAllDirections=false), la
                    // dirección siempre se resuelve a una cardinal: exacta si hay alineación y por
                    // cuadrante si no. Antes, un objetivo no alineado —o de pie sobre el centro de
                    // la Trampa Repulsiva con el Sram fuera de línea— no era empujado nunca.
                    if (castInfos.CellId != castInfos.Target.Cell.Id)
                    {
                        direction = Pathfinding.InLine(castInfos.Map, castInfos.CellId, castInfos.Target.Cell.Id)
                            ? Pathfinding.GetDirection(castInfos.Map, castInfos.CellId, castInfos.Target.Cell.Id)
                            : Pathfinding.GetCardinalDirection(castInfos.Map, castInfos.CellId, castInfos.Target.Cell.Id);
                    }
                    else if (castInfos.Caster.Cell != null && castInfos.Caster.Cell.Id != castInfos.Target.Cell.Id)
                    {
                        // Objetivo exactamente sobre el origen (p.ej. teletransportado al centro
                        // de la trampa): se empuja alejándolo del lanzador.
                        direction = Pathfinding.GetCardinalDirection(castInfos.Map, castInfos.Caster.Cell.Id, castInfos.Target.Cell.Id);
                    }
                    else
                    {
                        // Lanzador y objetivo en la misma celda (el Sram activa su propia trampa
                        // sobre el centro): sin dirección posible.
                        return FightActionResultEnum.RESULT_NOTHING;
                    }
                    break;

                case EffectEnum.MOVIMIENTO_ATRAER:
                    if (castInfos.Caster.Cell == null || castInfos.Caster.Cell.Id == castInfos.Target.Cell.Id)
                        return FightActionResultEnum.RESULT_NOTHING;

                    // Cardinal por cuadrante: la 8-direcciones podía devolver una diagonal y el
                    // objetivo se deslizaba en diagonal (movimiento ilegal en combate).
                    direction = Pathfinding.InLine(castInfos.Map, castInfos.Target.Cell.Id, castInfos.Caster.Cell.Id)
                        ? Pathfinding.GetDirection(castInfos.Map, castInfos.Target.Cell.Id, castInfos.Caster.Cell.Id)
                        : Pathfinding.GetCardinalDirection(castInfos.Map, castInfos.Target.Cell.Id, castInfos.Caster.Cell.Id);
                    break;
            }

            return ApplyPush(castInfos, castInfos.Target, direction, castInfos.Value1);
        }

        public static FightActionResultEnum ApplyPush(CastInfos castInfos, AbstractFighter target, DirectionEnum direction, int length)
        {
            if (IsGiantKralamar(target))
                return FightActionResultEnum.RESULT_NOTHING;

            var currentCell = target.Cell;

            for (int i = 0; i < length; i++)
            {
                // Paso validado: un empuje que alcanza el borde choca (colisión), no continúa por
                // la fila del otro extremo del mapa por el wrap-around de NextCell.
                var nextCell = Pathfinding.TryGetCellInDirection(castInfos.Map, currentCell.Id, direction, 1, out var nextCellId)
                    ? target.Fight.GetCell(nextCellId)
                    : null;

                if (nextCell != null && nextCell.CanWalk)
                {
                    if (nextCell.HasObject(FightObstacleTypeEnum.TYPE_TRAP))
                    {
                        target.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_PUSHBACK, target.Id, target.Id + "," + nextCell.Id));

                        target.Fight.SetSubAction(() => { return target.SetCell(nextCell); }, 1 + ++i * WorldConfig.FIGHT_PUSH_CELL_TIME);

                        return FightActionResultEnum.RESULT_NOTHING;
                    }
                }
                else
                {
                    if (i != 0)
                    {
                        target.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_PUSHBACK, target.Id, target.Id + "," + currentCell.Id));
                    }

                    target.Fight.SetSubAction(() =>
                    {
                        if (castInfos.EffectType == EffectEnum.MOVIMIENTO_EMPUJAR)
                        {
                            var pushResult = PushEffect.ApplyPushBackDamages(castInfos, target, length, i);
                            if (pushResult != FightActionResultEnum.RESULT_NOTHING)
                                return pushResult;
                        }

                        return target.SetCell(currentCell);
                    }, 1 + (i * WorldConfig.FIGHT_PUSH_CELL_TIME));

                    return FightActionResultEnum.RESULT_NOTHING;
                }

                currentCell = nextCell;
            }

            target.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_PUSHBACK, target.Id, target.Id + "," + currentCell.Id));

            target.Fight.SetSubAction(() => { return target.SetCell(currentCell); }, 1 + length * WorldConfig.FIGHT_PUSH_CELL_TIME);

            return FightActionResultEnum.RESULT_NOTHING;
        }

        private static FightActionResultEnum ApplyPushBackDamages(CastInfos castInfos, AbstractFighter target, int length, int currentLength)
        {
            var damageCoef = Util.Next(8, 17);
            double levelCoef = castInfos.Caster.Level / 50.0;

            if (levelCoef < 0.1)
                levelCoef = 0.1;

            int damageValue = (int)Math.Floor(damageCoef * levelCoef) * (length - currentLength);
            var subInfos = new CastInfos(EffectEnum.DANO_BRUTO, castInfos.SpellId, castInfos.CellId, 0, 0, 0, 0, 0, castInfos.Caster, null);

            return DamageEffect.ApplyDamages(subInfos, target, ref damageValue);
        }

        private static bool IsGiantKralamar(AbstractFighter target)
        {
            return (target as MonsterEntity)?.Grade?.MonsterId == 423;
        }
    }
}
