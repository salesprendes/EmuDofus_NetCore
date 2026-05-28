using Game.Fight.AI.Core;
using System.Collections.Generic;

namespace Game.Fight.AI.Bosses.Mechanics
{
    public sealed class RasboulMechanic : IBossMechanic
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            // TODO: wire Rasboul charge/vulnerability logic after confirming real states and spells.
            yield break;
        }
    }
}
