using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Sacrieur — Aggressive melee tank.
    ///
    /// Priority pipeline:
    ///   1. Self-buff (High) — Sacrieur blood mechanics reward being buffed
    ///   2. Kill shot (Critical)
    ///   3. Attack nearest / most vulnerable (high bonus for proximity + low HP)
    ///   4. Always close in — never flees
    ///
    /// Sacrieur accepts risk; no defensive movement.
    /// </summary>
    public sealed class DopeulSacrieurBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.Tank;
        protected override int         PreferredMinDistance => 1;
        protected override int         PreferredMaxDistance => 2;
        protected override bool        PrioritizeBuff       => true;
        protected override bool        Defensive            => false;   // never flees

        public DopeulSacrieurBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            // 1. Self-buff
            foreach (var decision in new BuffEvaluator().Evaluate(context))
            {
                if (decision.TargetId == context.Fighter?.Id)
                {
                    decision.Score    += 160;
                    decision.Priority  = AIDecisionPriority.High;
                    decision.Reason    = "Sacrieur self-buff";
                }
                yield return decision;
            }

            // 2. Kill shot
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 3. Attack — bonus for proximity and low enemy HP
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                var enemy = context.Enemies?.FirstOrDefault(e => e?.Id == decision.TargetId);
                var weakBonus = enemy != null ? TargetEvaluator.ScoreLowHp(enemy) : 0;
                var nearBonus = enemy?.Cell != null
                    ? System.Math.Max(0, 80 - context.TurnCache.Cells.GetDistance(context.CurrentCellId, enemy.Cell.Id) * 10)
                    : 0;
                decision.Score += 80 + weakBonus / 2 + nearBonus;
                yield return decision;
            }

            // 4. Always move closer — Sacrieur lives in melee range
            var movement = new MovementEvaluator();
            var nearCell = movement.GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
                yield return AIDecision.Move(nearCell.Value, 120, AIDecisionPriority.Low, "Sacrieur closing in");
        }
    }
}
