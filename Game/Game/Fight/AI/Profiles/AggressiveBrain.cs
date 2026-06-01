using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Profiles
{
    /// <summary>
    /// Aggressive AI profile — closes in and attacks as fast as possible.
    ///
    /// Decision pipeline per turn:
    ///   1. Attack from the current cell (High priority, +90 score bonus).
    ///   2. Move to the best attack position:
    ///        a. GetBestCellForAggressiveApproach — reachable cell that allows casting AND
    ///           is closest to an enemy (no risk penalty; uses full PM to get there).
    ///   3. Close in as far as possible even if no spell can be cast yet:
    ///        GetBestCellNearEnemy — uses ALL available PM to minimise distance to enemy.
    ///
    /// The two movement decisions can overlap (same destination cell).  SelectDecisions
    /// deduplicates by key so only the higher-scored one is kept — the aggressive-approach
    /// cell (High, 150) wins over the near-enemy cell (Low, 100) when they're the same.
    /// </summary>
    public sealed class AggressiveBrain : AIBrain
    {
        public AggressiveBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            // ── 1. Attack from current cell ───────────────────────────────────────────
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 90;
                if (decision.Priority == AIDecisionPriority.Normal)
                    decision.Priority = AIDecisionPriority.High;
                yield return decision;
            }

            var movement = new MovementEvaluator();

            // ── 2. Move to closest castable position (aggressive — no risk penalty) ──
            //
            // GetBestCellForAggressiveApproach picks the reachable cell from which the
            // fighter can cast a damage spell AND that puts it as close as possible to an
            // enemy.  Unlike GetBestCellForDistanceAttack, there is no -120 risk penalty
            // for melee-adjacent cells, so melee fighters correctly prefer walking all the
            // way to the adjacent cell rather than stopping short.
            var aggressiveCell = movement.GetBestCellForAggressiveApproach(context);
            if (aggressiveCell.HasValue)
                yield return AIDecision.Move(aggressiveCell.Value, 150, AIDecisionPriority.High, "Aggressive move to attack");

            // ── 3. Fallback: close in using full PM even if no spell can be cast ────
            //
            // This fires when no spell reaches any enemy from any reachable cell
            // (enemy is farther than all available MP + spell range).  The fighter still
            // advances as far as possible so next turn it can attack.
            var nearCell = movement.GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
                yield return AIDecision.Move(nearCell.Value, 100, AIDecisionPriority.Low, "Aggressive approach");
        }
    }
}
