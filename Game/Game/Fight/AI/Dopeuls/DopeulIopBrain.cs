using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    public sealed class DopeulIopBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role => DopeulRole.DamageMelee;
        protected override int PreferredMinDistance => 1;
        protected override int PreferredMaxDistance => 2;

        public DopeulIopBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {

            foreach (var decision in GetKillDecisions(context))
                yield return decision;


            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                var enemy = context.Enemies?.FirstOrDefault(e => e?.Id == decision.TargetId);
                var weakBonus = enemy != null ? TargetEvaluator.ScoreLowHp(enemy) / 2 : 0;
                decision.Score += 80 + weakBonus;
                yield return decision;
            }


            var movement = new MovementEvaluator();
            var nearCell = movement.GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
                yield return AIDecision.Move(nearCell.Value, 110, AIDecisionPriority.Low, "Yopuka se acerca cuerpo a cuerpo");
        }
    }
}
