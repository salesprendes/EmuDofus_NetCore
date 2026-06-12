using Game.Action;
using Game.Network;

namespace Game.Fight.Effect.Type
{
    public sealed class TeleportEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos CastInfos)
        {
            return ApplyTeleport(CastInfos);
        }

        public static FightActionResultEnum ApplyTeleport(CastInfos castInfos)
        {
            AbstractFighter lanzador = castInfos.Caster;

            if (lanzador.IsFighterDead || lanzador.StaticInvocation || lanzador.StateManager.HasState(FighterStateEnum.STATE_GRAVITY) || lanzador.StateManager.HasState(FighterStateEnum.STATE_ROOTED) || lanzador.StateManager.HasState(FighterStateEnum.STATE_CARRIED) || lanzador.StateManager.HasState(FighterStateEnum.STATE_CARRIER))
                return FightActionResultEnum.RESULT_NOTHING;

            FightCell cell = lanzador.Fight.GetCell(castInfos.CellId);

            if (!cell.CanWalk)
                return FightActionResultEnum.RESULT_NOTHING;

            if (cell != null)
            {
                lanzador.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_TELEPORT, lanzador.Id, lanzador.Id + "," + castInfos.CellId));

                return lanzador.SetCell(cell);
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


