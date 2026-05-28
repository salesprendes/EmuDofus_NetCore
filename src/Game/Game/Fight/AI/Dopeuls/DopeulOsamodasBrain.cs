using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Osamodas — Summoner + ally support.
    ///
    /// Priority pipeline:
    ///   1. Invoke (High) — maximize creatures on the field
    ///   2. Heal own summons if injured (&lt; 60 % HP)
    ///   3. Buff summons / allies (High)
    ///   4. Kill shot (Critical)
    ///   5. Attack as fallback
    ///   6. Preferred medium distance
    /// </summary>
    public sealed class DopeulOsamodasBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.Summoner;
        protected override int         PreferredMinDistance => 3;
        protected override int         PreferredMaxDistance => 7;
        protected override bool        PrioritizeBuff       => true;

        public DopeulOsamodasBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            // 1. Invoke
            foreach (var decision in new SummonEvaluator().Evaluate(context))
            {
                decision.Score    += 160;
                decision.Priority  = AIDecisionPriority.High;
                yield return decision;
            }

            // 2. Heal injured summons
            if (context.SpellBook?.HealSpells?.Count > 0)
            {
                var injuredSummon = context.Allies?
                    .Where(a => a?.Invocator == context.Fighter && !a.IsFighterDead
                             && a.MaxLife > 0 && (double)a.Life / a.MaxLife < 0.60)
                    .OrderBy(a => (double)a.Life / a.MaxLife)
                    .FirstOrDefault();

                if (injuredSummon != null)
                {
                    foreach (var decision in new HealEvaluator().Evaluate(context))
                    {
                        if (decision.TargetId == injuredSummon.Id)
                        {
                            decision.Score    += 200;
                            decision.Priority  = AIDecisionPriority.High;
                            decision.Reason    = "Osamodas healing summon";
                        }
                        yield return decision;
                    }
                }
            }

            // 3. Buff summons / allies
            foreach (var decision in new BuffEvaluator().Evaluate(context))
            {
                var isAlly = context.Allies?.Any(a => a?.Id == decision.TargetId) ?? false;
                if (isAlly)
                {
                    decision.Score    += 100;
                    decision.Priority  = AIDecisionPriority.High;
                }
                yield return decision;
            }

            // 4. Kill shot
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 5. Attack fallback
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 30;
                yield return decision;
            }

            // 6. Preferred distance
            var movement = new MovementEvaluator();
            var target   = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(
                    context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Osamodas preferred distance");
            }
        }
    }
}
