using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Cra — Distance damage dealer.
    ///
    /// Priority pipeline:
    ///   1. Flee (High) if nearest enemy is inside preferred min distance
    ///   2. Kill shot (Critical)
    ///   3. MP-removal debuffs (High) — slows enemy approach
    ///   4. Other debuffs / attack from distance
    ///   5. Move to preferred distance band
    /// </summary>
    public sealed class DopeulCraBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.DamageDistance;
        protected override int         PreferredMinDistance => 5;
        protected override int         PreferredMaxDistance => 10;
        protected override bool        Defensive            => true;

        public DopeulCraBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();

            // 1. Flee if an enemy is too close
            if (GetNearestEnemyDistance(context) < PreferredMinDistance && context.CurrentMP > 0)
            {
                var awayCell = movement.GetBestCellAwayFromEnemies(context);
                if (awayCell.HasValue)
                    yield return AIDecision.Move(awayCell.Value, 250, AIDecisionPriority.High, "Cra fleeing — enemy too close");
            }

            // 2. Kill shot
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 3. MP-removal debuffs (prioritised to slow enemy advance)
            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                var spell = context.SpellBook?.AllSpells?.FirstOrDefault(s => s?.SpellId == decision.SpellId);
                if (spell != null && AISpellBook.HasRemoveMPEffect(spell))
                {
                    decision.Score    += 130;
                    decision.Priority  = AIDecisionPriority.High;
                    decision.Reason    = "Cra MP removal";
                }
                yield return decision;
            }

            // 4. Attack from distance
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 40;
                yield return decision;
            }

            // 5. Maintain preferred distance
            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(
                    context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Cra maintaining distance");
            }
        }
    }
}
