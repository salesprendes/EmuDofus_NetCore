using Game.Fight.AI.Core;
using Game.Map;

namespace Game.Fight.AI.Actions
{
    public sealed class MoveToCellAIAction : IAIAction
    {
        private readonly AIDecision m_decision;
        private string m_path;
        private int m_delay;
        private int m_startCell = -1;

        public AIDecisionType Type => AIDecisionType.Move;
        public int EstimatedDelayMs => m_delay > 0 ? m_delay : WorldConfig.FIGHT_AI_MOVE_DELAY;

        public MoveToCellAIAction(AIDecision decision)
        {
            m_decision = decision;
        }

        public bool CanExecute(AIContext context)
        {
            return TryPreparePath(context);
        }

        public AIActionResult Execute(AIContext context)
        {
            if (!IsPreparedPathStillValid(context) && !TryPreparePath(context))
                return AIActionResult.Fail("El movimiento ya no es valido");

            context.Fight.Move(context.Fighter, context.Fighter.Cell.Id, m_path);
            return AIActionResult.Ok(EstimatedDelayMs, "Move queued");
        }

        private bool TryPreparePath(AIContext context)
        {
            if (context?.Fighter == null || context.Fight == null || m_decision?.CellId == null)
                return false;

            m_path = string.Empty;
            m_delay = 0;
            m_startCell = -1;

            if (context.Fighter.IsFighterDead
                || context.Fighter.Cell == null
                || context.Fighter.MP <= 0
                || !context.Fighter.CanBeMoved())
                return false;

            var targetCell = m_decision.CellId.Value;
            if (targetCell == context.Fighter.Cell.Id)
                return false;

            var fightCell = context.Fight.GetCell(targetCell);
            if (fightCell == null || !fightCell.CanWalk)
                return false;

            // Primero un camino que termine EXACTAMENTE en la celda planificada (evita que el
            // luchador derive a celdas no planificadas). Si no existe, un camino de aproximacion
            // que el motor puede truncar en una trampa: el monstruo avanza y la PISA en vez de
            // descartar el movimiento y pasar turno.
            var cells = context.TurnCache?.Cells;
            m_path = cells?.GetExactPathToCell(targetCell);
            if (string.IsNullOrEmpty(m_path))
                m_path = cells?.GetApproachPathToCell(targetCell);
            if (string.IsNullOrEmpty(m_path))
                return false;

            var movementPath = Pathfinding.IsValidPath(context.Fight, context.Fighter, context.Fighter.Cell.Id, m_path);
            if (!IsUsablePath(context, movementPath, targetCell))
                return false;

            m_startCell = context.Fighter.Cell.Id;
            m_delay = System.Math.Max(1, (int)System.Math.Ceiling(movementPath.MovementTime) + WorldConfig.FIGHT_AI_MOVE_DELAY);
            return true;
        }

        private bool IsPreparedPathStillValid(AIContext context)
        {
            if (context?.Fighter == null
                || context.Fight == null
                || context.Fighter.Cell == null
                || string.IsNullOrEmpty(m_path)
                || m_startCell != context.Fighter.Cell.Id
                || m_decision?.CellId == null)
                return false;

            var movementPath = Pathfinding.IsValidPath(context.Fight, context.Fighter, context.Fighter.Cell.Id, m_path);
            if (!IsUsablePath(context, movementPath, m_decision.CellId.Value))
                return false;

            m_delay = System.Math.Max(1, (int)System.Math.Ceiling(movementPath.MovementTime) + WorldConfig.FIGHT_AI_MOVE_DELAY);
            return true;
        }

        private static bool IsUsablePath(AIContext context, MovementPath movementPath, int targetCell)
        {
            if (movementPath == null
                || movementPath.MovementLength <= 0
                || movementPath.MovementLength > context.Fighter.MP
                || movementPath.EndCell == context.Fighter.Cell.Id)
                return false;

            // Vale si llega al objetivo, o si el motor lo trunca en una stop cell (trampa oculta o
            // casilla pegada a un enemigo): el luchador avanza y la pisa. Un corte en cualquier
            // otra celda (p. ej. el Pathmaker rindiendose ante un muro) se rechaza para no derivar.
            return movementPath.EndCell == targetCell
                || Pathfinding.IsStopCell(context.Fight, context.Fighter.Team, movementPath.EndCell);
        }
    }
}
