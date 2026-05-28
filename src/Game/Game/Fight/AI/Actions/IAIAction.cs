using Game.Fight.AI.Core;

namespace Game.Fight.AI.Actions
{
    public interface IAIAction
    {
        AIDecisionType Type { get; }
        int EstimatedDelayMs { get; }
        bool CanExecute(AIContext context);
        AIActionResult Execute(AIContext context);
    }
}
