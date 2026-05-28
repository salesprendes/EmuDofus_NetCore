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
                return AIActionResult.Fail("Move no longer valid");

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

            try
            {
                m_path = context.Fight.Map?.Pathmaker?.FindPathAsString(
                    context.Fighter.Cell.Id,
                    targetCell,
                    false,
                    context.Fighter.MP,
                    context.Fight.Obstacles) ?? string.Empty;
            }
            catch
            {
                m_path = string.Empty;
            }

            if (string.IsNullOrEmpty(m_path))
                return false;

            var movementPath = Pathfinding.IsValidPath(context.Fight, context.Fighter, context.Fighter.Cell.Id, m_path);
            if (movementPath == null || movementPath.MovementLength <= 0 || movementPath.MovementLength > context.Fighter.MP)
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
                || m_startCell != context.Fighter.Cell.Id)
                return false;

            var movementPath = Pathfinding.IsValidPath(context.Fight, context.Fighter, context.Fighter.Cell.Id, m_path);
            if (movementPath == null || movementPath.MovementLength <= 0 || movementPath.MovementLength > context.Fighter.MP)
                return false;

            m_delay = System.Math.Max(1, (int)System.Math.Ceiling(movementPath.MovementTime) + WorldConfig.FIGHT_AI_MOVE_DELAY);
            return true;
        }
    }
}
