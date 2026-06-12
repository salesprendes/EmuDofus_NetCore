using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Dopeuls
{
    public sealed class DopeulEniripsaBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role => DopeulRole.Healer;
        protected override int PreferredMinDistance => 3;
        protected override int PreferredMaxDistance => 7;
        protected override bool Defensive => true;

        public DopeulEniripsaBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();


            if (IsSelfLowHP(context, LowHpThreshold) && context.CurrentMP > 0)
            {
                var awayCell = movement.GetBestCellAwayFromEnemies(context);
                if (awayCell.HasValue)
                    yield return AIDecision.Move(awayCell.Value, 200, AIDecisionPriority.Critical, "Eniripsa huye (vida critica)");
            }


            foreach (var decision in new HealEvaluator().Evaluate(context))
            {
                if (decision.TargetId == context.Fighter?.Id && IsSelfLowHP(context, SelfHealThreshold))
                {
                    decision.Score += 300;
                    decision.Priority = AIDecisionPriority.Critical;
                    decision.Reason = "Eniripsa self-heal (low HP)";
                }
                else
                {
                    decision.Score += 150;
                }
                yield return decision;
            }


            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 20;
                yield return decision;
            }


            var awayFromEnemies = movement.GetBestCellAwayFromEnemies(context);
            if (awayFromEnemies.HasValue)
                yield return AIDecision.Move(awayFromEnemies.Value, 120, AIDecisionPriority.Normal, "Eniripsa posicionamiento defensivo");

            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 80, AIDecisionPriority.Low, "Eniripsa distancia preferida");
            }
        }
    }
}
