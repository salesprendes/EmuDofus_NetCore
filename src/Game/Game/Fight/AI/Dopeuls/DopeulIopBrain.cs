using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Dopeul Iop — Pure melee damage dealer.
    ///
    /// Priority pipeline:
    ///   1. Kill shot (Critical) — finishes off any killable enemy
    ///   2. Attack with bonus for weakest enemies
    ///   3. Move to melee range
    ///
    /// Iop never flees and does not spend PA on buffs or debuffs unless a kill is available.
    /// </summary>
    public sealed class DopeulIopBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role               => DopeulRole.DamageMelee;
        protected override int         PreferredMinDistance => 1;
        protected override int         PreferredMaxDistance => 2;

        public DopeulIopBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            // 1. Kill priority — always rematar si es posible
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 2. Attack — bonus for low-HP targets (weakest first)
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                var enemy = context.Enemies?.FirstOrDefault(e => e?.Id == decision.TargetId);
                var weakBonus = enemy != null ? TargetEvaluator.ScoreLowHp(enemy) / 2 : 0;
                decision.Score += 80 + weakBonus;
                yield return decision;
            }

            // 3. Move to melee range
            var movement  = new MovementEvaluator();
            var nearCell  = movement.GetBestCellNearEnemy(context);
            if (nearCell.HasValue)
                yield return AIDecision.Move(nearCell.Value, 110, AIDecisionPriority.Low, "Iop approaching for melee");
        }
    }
}
