using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    public sealed class DopeulPandawaBrain : BaseDopeulBrain
    {
        protected override DopeulRole Role => DopeulRole.Controller;
        protected override int PreferredMinDistance => 2;
        protected override int PreferredMaxDistance => 6;
        protected override bool PrioritizeDebuff => true;

        public DopeulPandawaBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();
            var enemies = context.Enemies;


            if (context.SpellBook?.PushPullSpells?.Count > 0 && enemies != null)
            {
                foreach (var spell in context.SpellBook.PushPullSpells)
                {
                    if (spell == null || spell.APCost > context.CurrentAP)
                        continue;

                    foreach (var enemy in enemies)
                    {
                        if (enemy?.Cell == null || enemy.IsFighterDead)
                            continue;

                        if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, enemy.Cell.Id))
                            continue;

                        var areaScore = SpellEvaluator.ScoreAreaImpact(context, spell, enemy.Cell.Id, false);
                        yield return new AIDecision
                        {
                            Type = AIDecisionType.CastSpell,
                            Priority = AIDecisionPriority.High,
                            Score = 160 + TargetEvaluator.ScorePriorityTarget(enemy) / 3 + areaScore,
                            SpellId = spell.SpellId,
                            TargetId = enemy.Id,
                            CellId = (short)enemy.Cell.Id,
                            Reason = "Pandawa push/pull repositioning"
                        };
                    }
                }
            }


            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                var spell = context.SpellBook?.AllSpells?.FirstOrDefault(s => s?.SpellId == decision.SpellId);
                var isVuln = spell != null && AISpellBook.HasVulnerabilityEffect(spell);

                if (isVuln)
                {
                    decision.Score += 140;
                    decision.Priority = AIDecisionPriority.High;
                    decision.Reason = "Pandawa vulnerability debuff";
                }
                else
                {
                    decision.Score += 70;
                }
                yield return decision;
            }


            foreach (var decision in GetKillDecisions(context))
                yield return decision;


            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += 40;
                yield return decision;
            }


            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = movement.GetBestCellForPreferredDistance(context, target, PreferredMinDistance, PreferredMaxDistance);
                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Pandawa distancia preferida");
            }
        }
    }
}
