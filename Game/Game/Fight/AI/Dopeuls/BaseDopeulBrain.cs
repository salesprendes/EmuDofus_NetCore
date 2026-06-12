using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    public abstract class BaseDopeulBrain : AIBrain
    {

        protected abstract DopeulRole Role { get; }
        protected virtual int PreferredMinDistance => 1;
        protected virtual int PreferredMaxDistance => 6;
        protected virtual bool PreferMelee => Role == DopeulRole.DamageMelee || Role == DopeulRole.Tank;


        protected virtual bool PrioritizeHealing => Role == DopeulRole.Healer;
        protected virtual bool PrioritizeSummon => Role == DopeulRole.Summoner;
        protected virtual bool PrioritizeBuff => Role == DopeulRole.Support || Role == DopeulRole.Tank;
        protected virtual bool PrioritizeDebuff => Role == DopeulRole.Debuffer || Role == DopeulRole.Controller;
        protected virtual bool Defensive => false;


        protected const double SelfHealThreshold = 0.50;
        protected const double LowHpThreshold = 0.30;

        protected BaseDopeulBrain(AIFighter fighter) : base(fighter) { }










        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();


            if (Defensive && IsSelfLowHP(context, LowHpThreshold) && context.CurrentMP > 0)
            {
                var awayCell = movement.GetBestCellAwayFromEnemies(context);
                if (awayCell.HasValue)
                    yield return AIDecision.Move(awayCell.Value, 200, AIDecisionPriority.Critical, "Huida por autoproteccion");
            }


            foreach (var decision in GetKillDecisions(context))
                yield return decision;


            if (PrioritizeHealing)
            {
                foreach (var decision in new HealEvaluator().Evaluate(context))
                {
                    decision.Score += 150;
                    yield return decision;
                }
            }

            if (PrioritizeSummon)
            {
                foreach (var decision in new SummonEvaluator().Evaluate(context))
                {
                    decision.Score += 150;
                    decision.Priority = AIDecisionPriority.High;
                    yield return decision;
                }
            }

            if (PrioritizeBuff)
            {
                foreach (var decision in new BuffEvaluator().Evaluate(context))
                {
                    decision.Score += 80;
                    yield return decision;
                }
            }

            if (PrioritizeDebuff)
            {
                foreach (var decision in new DebuffEvaluator().Evaluate(context))
                {
                    decision.Score += 100;
                    decision.Priority = AIDecisionPriority.High;
                    yield return decision;
                }
            }


            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += PreferMelee ? 70 : 40;
                yield return decision;
            }


            if (Defensive)
            {
                var awayCell = movement.GetBestCellAwayFromEnemies(context);
                if (awayCell.HasValue)
                    yield return AIDecision.Move(awayCell.Value, 120, AIDecisionPriority.Normal, "Dopeul movimiento defensivo");
            }

            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = PreferMelee ? movement.GetBestCellNearEnemy(context) : movement.GetBestCellForPreferredDistance(context, target, PreferredMinDistance, PreferredMaxDistance);

                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Dopeul distancia preferida");
            }

            foreach (var decision in new MovementEvaluator().Evaluate(context))
                yield return decision;
        }





        protected IEnumerable<AIDecision> GetKillDecisions(AIContext context)
        {
            if (context?.SpellBook?.DamageSpells == null || context.Enemies == null)
                yield break;

            foreach (var spell in context.SpellBook.DamageSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                var damage = SpellEvaluator.EstimateDamage(spell);

                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                        continue;

                    var killScore = TargetEvaluator.ScoreKillChance(context.Fighter, enemy, damage);
                    if (killScore <= 0)
                        continue;

                    if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, enemy.Cell.Id))
                        continue;

                    yield return new AIDecision
                    {
                        Type = AIDecisionType.CastSpell,
                        Priority = AIDecisionPriority.Critical,
                        Score = 500 + killScore + TargetEvaluator.ScoreLowHp(enemy),
                        SpellId = spell.SpellId,
                        TargetId = enemy.Id,
                        CellId = (short)enemy.Cell.Id,
                        Reason = "Kill shot"
                    };
                }
            }
        }

        protected static bool IsSelfLowHP(AIContext context, double threshold)
        {
            var fighter = context?.Fighter;
            return fighter != null && fighter.MaxLife > 0 && (double)fighter.Life / fighter.MaxLife < threshold;
        }

        protected static int GetNearestEnemyDistance(AIContext context)
        {
            if (context?.EnemyTargets == null || context.EnemyTargets.Count == 0)
                return int.MaxValue;

            return context.EnemyTargets[0].Distance;
        }




        protected override void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
                return;

            Logger.Debug("[IA][Dopeul] Luchador=" + (Fighter?.Id ?? 0)
                + " Clase=" + GetType().Name
                + " Rol=" + Role
                + " Decision=" + decision.Type
                + " Prioridad=" + decision.Priority
                + " Puntuacion=" + decision.Score
                + " Motivo=" + decision.Reason);
        }
    }
}
