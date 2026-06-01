using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Bosses
{
    public sealed class RoyalBlopBrain : AIBrain
    {
        public RoyalBlopBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                if (decision.SpellId.HasValue && context.SpellBook.RemoveMPSpells.Contains(context.Fighter.SpellBook.GetSpellLevel(decision.SpellId.Value)))
                    decision.Score += 100;
                yield return decision;
            }

            foreach (var decision in new AttackEvaluator().Evaluate(context))
                yield return decision;

            var movement = new MovementEvaluator();
            var nearest = TargetEvaluator.GetNearestEnemy(context);
            if (nearest?.Cell != null)
            {
                var distance = context.TurnCache.Cells.GetDistance(context.CurrentCellId, nearest.Cell.Id);
                if (distance <= 2)
                {
                    var away = movement.GetBestCellAwayFromEnemies(context);
                    if (away.HasValue)
                        yield return AIDecision.Move(away.Value, 140, AIDecisionPriority.High, "Royal Blop keeps mid range");
                }
            }
        }
    }
}
