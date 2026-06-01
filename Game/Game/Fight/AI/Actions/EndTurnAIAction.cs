using Game.Fight.AI.Core;

namespace Game.Fight.AI.Actions
{
    public sealed class EndTurnAIAction : IAIAction
    {
        private readonly AIDecision m_decision;

        public AIDecisionType Type => AIDecisionType.EndTurn;
        public int EstimatedDelayMs => 0;

        public EndTurnAIAction(AIDecision decision)
        {
            m_decision = decision;
        }

        public bool CanExecute(AIContext context)
        {
            return context?.Fighter != null;
        }

        public AIActionResult Execute(AIContext context)
        {
            if (context?.Fighter == null)
                return AIActionResult.Fail("No fighter to end turn");

            context.Fighter.TurnPass = true;
            return AIActionResult.EndTurn(m_decision?.Reason ?? "End turn");
        }
    }
}
