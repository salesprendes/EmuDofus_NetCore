using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    public sealed class DopeulFecaBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role => DopeulRole.Support;
        protected override int PreferredMinDistance => 2;
        protected override int PreferredMaxDistance => 5;
        protected override bool PrioritizeBuff => true;

        public DopeulFecaBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();


            var adjacentEnemies = context.EnemyTargets?.Count(t => t.Distance <= 2) ?? 0;
            if (adjacentEnemies >= 2 && context.CurrentMP > 0)
            {
                var awayCell = movement.GetBestCellAwayFromEnemies(context);
                if (awayCell.HasValue)
                    yield return AIDecision.Move(awayCell.Value, 180, AIDecisionPriority.High, "Feca huye - rodeado");
            }


            foreach (var decision in new BuffEvaluator().Evaluate(context))
            {
                decision.Score += 120;
                decision.Priority = AIDecisionPriority.High;
                yield return decision;
            }


            foreach (var decision in GetKillDecisions(context))
                yield return decision;


            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 30;
                yield return decision;
            }


            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Feca distancia preferida");
            }
        }
    }
}
