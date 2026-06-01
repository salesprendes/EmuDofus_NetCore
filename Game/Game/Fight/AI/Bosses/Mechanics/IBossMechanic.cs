using Game.Fight.AI.Core;
using System.Collections.Generic;

namespace Game.Fight.AI.Bosses.Mechanics
{
    public interface IBossMechanic
    {
        IEnumerable<AIDecision> Evaluate(AIContext context);
    }
}
