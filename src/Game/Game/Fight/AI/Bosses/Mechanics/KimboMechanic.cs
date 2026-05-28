using Game.Fight.AI.Core;
using System.Collections.Generic;

namespace Game.Fight.AI.Bosses.Mechanics
{
    public sealed class KimboMechanic : IBossMechanic
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            // TODO: wire Kimbo glyph/state logic when states and fight metadata expose the required data.
            yield break;
        }
    }
}
