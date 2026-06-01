using Game.Fight.AI.Core;
using System.Collections.Generic;

namespace Game.Fight.AI.Evaluation
{
    public interface IAIEvaluator
    {
        IEnumerable<AIDecision> Evaluate(AIContext context);
    }
}
