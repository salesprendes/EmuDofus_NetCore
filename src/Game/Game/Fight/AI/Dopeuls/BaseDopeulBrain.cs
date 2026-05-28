using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Dopeuls
{
    /// <summary>
    /// Base class for all Dopeul AI brains.
    ///
    /// Properties subclasses can override:
    ///   Role                — semantic role that drives default flags
    ///   PreferredMinDistance — ideal minimum range from enemies
    ///   PreferredMaxDistance — ideal maximum range from enemies
    ///   PreferMelee         — derived from Role; subclass can force true/false
    ///   PrioritizeHealing   — yield healing decisions with bonus score first
    ///   PrioritizeSummon    — yield summon decisions with bonus score first
    ///   PrioritizeBuff      — yield buff decisions with bonus score first
    ///   PrioritizeDebuff    — yield debuff decisions with bonus score + High priority
    ///   Defensive           — flee when enemies approach and yield away-cell movement
    ///
    /// Helper methods subclasses can call:
    ///   GetKillDecisions(context)              — Critical-priority attack if kill possible
    ///   IsSelfLowHP(context, threshold)       — checks HP ratio
    ///   GetNearestEnemyDistance(context)      — distance to closest enemy
    /// </summary>
    public abstract class BaseDopeulBrain : AIBrain
    {
        // ── Role and distance ────────────────────────────────────────────────────
        protected abstract DopeulRole Role { get; }
        protected virtual int PreferredMinDistance => 1;
        protected virtual int PreferredMaxDistance => 6;
        protected virtual bool PreferMelee => Role == DopeulRole.DamageMelee || Role == DopeulRole.Tank;

        // ── Priority flags ────────────────────────────────────────────────────────
        protected virtual bool PrioritizeHealing => Role == DopeulRole.Healer;
        protected virtual bool PrioritizeSummon  => Role == DopeulRole.Summoner;
        protected virtual bool PrioritizeBuff    => Role == DopeulRole.Support || Role == DopeulRole.Tank;
        protected virtual bool PrioritizeDebuff  => Role == DopeulRole.Debuffer || Role == DopeulRole.Controller;
        protected virtual bool Defensive         => false;

        // ── HP thresholds ─────────────────────────────────────────────────────────
        protected const double SelfHealThreshold = 0.50;   // self-heal when HP < 50%
        protected const double LowHpThreshold    = 0.30;   // flee when HP < 30%

        protected BaseDopeulBrain(AIFighter fighter) : base(fighter) { }

        // ─────────────────────────────────────────────────────────────────────────
        // Default Evaluate — used by any Dopeul that does NOT override it.
        // The ordered pipeline is:
        //   1. Self-protection flee (Critical)
        //   2. Kill shots (Critical)
        //   3. Role priorities: Heal → Summon → Buff → Debuff
        //   4. Attack
        //   5. Movement (defensive away / preferred distance / generic)
        // ─────────────────────────────────────────────────────────────────────────
        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var movement = new MovementEvaluator();

            // 1. Self-protection
            if (Defensive && IsSelfLowHP(context, LowHpThreshold) && context.CurrentMP > 0)
            {
                var awayCell = movement.GetBestCellAwayFromEnemies(context);
                if (awayCell.HasValue)
                    yield return AIDecision.Move(awayCell.Value, 200, AIDecisionPriority.Critical, "Self-protection flee");
            }

            // 2. Kill shots
            foreach (var decision in GetKillDecisions(context))
                yield return decision;

            // 3. Role-priority evaluations
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

            // 4. Attack
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += PreferMelee ? 70 : 40;
                yield return decision;
            }

            // 5. Movement
            if (Defensive)
            {
                var awayCell = movement.GetBestCellAwayFromEnemies(context);
                if (awayCell.HasValue)
                    yield return AIDecision.Move(awayCell.Value, 120, AIDecisionPriority.Normal, "Dopeul defensive movement");
            }

            var target = TargetEvaluator.GetNearestEnemy(context);
            if (target?.Cell != null)
            {
                var preferredCell = PreferMelee
                    ? movement.GetBestCellNearEnemy(context)
                    : movement.GetBestCellForPreferredDistance(context, target, PreferredMinDistance, PreferredMaxDistance);

                if (preferredCell.HasValue)
                    yield return AIDecision.Move(preferredCell.Value, 100, AIDecisionPriority.Low, "Dopeul preferred distance");
            }

            foreach (var decision in new MovementEvaluator().Evaluate(context))
                yield return decision;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Helpers available to subclasses
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates Critical-priority CastSpell decisions for any enemy that can be
        /// killed from the current cell with a damage spell.
        /// </summary>
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
                        Type     = AIDecisionType.CastSpell,
                        Priority = AIDecisionPriority.Critical,
                        Score    = 500 + killScore + TargetEvaluator.ScoreLowHp(enemy),
                        SpellId  = spell.SpellId,
                        TargetId = enemy.Id,
                        CellId   = (short)enemy.Cell.Id,
                        Reason   = "Kill shot"
                    };
                }
            }
        }

        /// <summary>Returns true if this fighter's life ratio is below the given threshold (0.0–1.0).</summary>
        protected static bool IsSelfLowHP(AIContext context, double threshold)
        {
            var fighter = context?.Fighter;
            return fighter != null && fighter.MaxLife > 0
                && (double)fighter.Life / fighter.MaxLife < threshold;
        }

        /// <summary>Distance to the closest enemy (pre-computed in EnemyTargets). Returns int.MaxValue if none.</summary>
        protected static int GetNearestEnemyDistance(AIContext context)
        {
            if (context?.EnemyTargets == null || context.EnemyTargets.Count == 0)
                return int.MaxValue;

            return context.EnemyTargets[0].Distance;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Logging
        // ─────────────────────────────────────────────────────────────────────────
        protected override void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
                return;

            Logger.Debug("[AI][Dopeul] Fighter=" + (Fighter?.Id ?? 0)
                + " Class=" + GetType().Name
                + " Role=" + Role
                + " Decision=" + decision.Type
                + " Priority=" + decision.Priority
                + " Score=" + decision.Score
                + " Reason=" + decision.Reason);
        }
    }
}
