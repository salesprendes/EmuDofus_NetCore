using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Feca — Support / defensive buffer.
    ///
    /// Priority pipeline:
    ///   1. Escape (High) if surrounded by 2+ adjacent enemies
    ///   2. Defensive buff priority (High) — glyphs, shields, etc.
    ///   3. Kill shot (Critical)
    ///   4. Attack
    ///   5. Preferred positioning
    /// </summary>
    public sealed class DopeulFecaBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.Support;
        protected override int         PreferredMinDistance => 2;
        protected override int         PreferredMaxDistance => 5;
        protected override bool        PrioritizeBuff       => true;

        public DopeulFecaBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();

            // 1. Escape if surrounded (>= 2 enemies within distance 2)
            var adjacentEnemies = context.EnemyTargets?.Count(t => t.Distance <= 2) ?? 0;
            if (adjacentEnemies >= 2 && context.CurrentMP > 0)
            {
                var awayCell = movement.GetBestCellAwayFromEnemies(context);
                if (awayCell.HasValue)
                    yield return AIDecision.Move(awayCell.Value, 180, AIDecisionPriority.High, "Feca escaping — surrounded");
            }

            // 2. Buffs — Feca needs glyphs and defensive spells early
            foreach (var decision in new BuffEvaluator().Evaluate(context))
            {
                decision.Score    += 120;
                decision.Priority  = AIDecisionPriority.High;
                yield return decision;
            }

            // 3. Kill shot
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 4. Attack
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 30;
                yield return decision;
            }

            // 5. Preferred positioning
            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(
                    context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Feca preferred distance");
            }
        }
    }
}
