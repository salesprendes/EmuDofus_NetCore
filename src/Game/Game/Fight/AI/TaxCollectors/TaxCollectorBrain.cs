using Game.Fight;
using Game.Fight.AI.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.TaxCollectors
{
    public sealed class TaxCollectorBrain : AIBrain
    {
        private TaxCollectorDefenseMode m_lastMode;

        public TaxCollectorBrain(AIFighter fighter) : base(fighter)
        {
            m_lastMode = TaxCollectorDefenseMode.Normal;
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (!HasUsableContext(context))
            {
                yield return AIDecision.EndTurn("TaxCollector invalid context");
                yield break;
            }

            m_lastMode = CalculateDefenseMode(context);
            ConfigureBudget(context, m_lastMode);

            if (m_lastMode == TaxCollectorDefenseMode.CannotAct)
            {
                yield return AIDecision.EndTurn("TaxCollector cannot act");
                yield break;
            }

            if (context.Enemies == null || context.Enemies.Count == 0)
            {
                yield return AIDecision.EndTurn("TaxCollector has no attackers");
                yield break;
            }

            var evaluator = new TaxCollectorEvaluator(m_lastMode);
            var decisions = evaluator.Evaluate(context)
                .Where(decision => IsAllowedDecision(context, decision))
                .ToList();

            if (decisions.Count == 0)
            {
                yield return AIDecision.EndTurn("TaxCollector no useful action");
                yield break;
            }

            foreach (var decision in decisions)
                yield return decision;
        }

        protected override IEnumerable<AIDecision> GetFallbackDecisions(AIContext context)
        {
            yield return AIDecision.EndTurn("TaxCollector fallback end turn");
        }

        protected override void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
                return;

            Logger.Debug("[AI][TaxCollector] Fighter=" + (Fighter?.Id ?? 0)
                + " Mode=" + m_lastMode
                + " Decision=" + decision.Type
                + " Priority=" + decision.Priority
                + " Score=" + decision.Score
                + " Spell=" + (decision.SpellId?.ToString() ?? "-")
                + " Target=" + (decision.TargetId?.ToString() ?? "-")
                + " Reason=" + decision.Reason);
        }

        private static TaxCollectorDefenseMode CalculateDefenseMode(AIContext context)
        {
            if (!HasUsableContext(context))
                return TaxCollectorDefenseMode.CannotAct;

            if (context.CurrentAP <= 0 || context.SpellBook?.AllSpells == null || context.SpellBook.AllSpells.Count == 0)
                return TaxCollectorDefenseMode.CannotAct;

            var fighter = context.Fighter;
            var hpRatio = fighter.MaxLife > 0 ? (double)fighter.Life / fighter.MaxLife : 1.0;
            if (hpRatio <= 0.30)
                return TaxCollectorDefenseMode.LowHealth;

            var adjacentEnemies = 0;
            var closeEnemies = 0;
            var nearestEnemyDistance = int.MaxValue;

            foreach (var enemy in context.Enemies ?? Enumerable.Empty<AbstractFighter>())
            {
                if (enemy?.Cell == null || enemy.IsFighterDead)
                    continue;

                var distance = context.TurnCache.Cells.GetDistance(context.CurrentCellId, enemy.Cell.Id);
                if (distance < nearestEnemyDistance)
                    nearestEnemyDistance = distance;

                if (distance <= 1)
                    adjacentEnemies++;
                if (distance <= 2)
                    closeEnemies++;
            }

            if (adjacentEnemies >= 2 || closeEnemies >= 3)
                return TaxCollectorDefenseMode.Surrounded;

            var livingDefenders = context.Allies?
                .Count(ally => ally != null && ally != fighter && !ally.IsFighterDead) ?? 0;

            if (livingDefenders == 0)
                return TaxCollectorDefenseMode.NoDefenders;

            if (adjacentEnemies > 0 || nearestEnemyDistance <= 3)
                return TaxCollectorDefenseMode.UnderPressure;

            return TaxCollectorDefenseMode.Normal;
        }

        private static void ConfigureBudget(AIContext context, TaxCollectorDefenseMode mode)
        {
            if (context?.Budget == null)
                return;

            var canMove = CanMove(context) && mode != TaxCollectorDefenseMode.CannotAct;
            context.Budget.MaxMovements = canMove ? Math.Min(context.Budget.MaxMovements, 1) : 0;
        }

        private static bool IsAllowedDecision(AIContext context, AIDecision decision)
        {
            if (decision == null || !decision.IsValid)
                return false;

            if (decision.Type == AIDecisionType.EndTurn)
                return true;

            if (decision.Type == AIDecisionType.Move)
                return CanMove(context);

            if (IsSpellDecision(decision.Type))
            {
                if (!decision.SpellId.HasValue)
                    return false;

                var spell = context?.Fighter?.SpellBook?.GetSpellLevel(decision.SpellId.Value);
                if (spell == null || spell.APCost > (context?.CurrentAP ?? 0))
                    return false;

                if (decision.TargetId.HasValue && !IsTargetAlive(context, decision.TargetId.Value))
                    return false;
            }

            return true;
        }

        private static bool IsTargetAlive(AIContext context, long targetId)
        {
            return context?.Allies?.Any(f => f != null && f.Id == targetId && !f.IsFighterDead) == true
                || context?.Enemies?.Any(f => f != null && f.Id == targetId && !f.IsFighterDead) == true;
        }

        private static bool IsSpellDecision(AIDecisionType type)
        {
            return type == AIDecisionType.CastSpell
                || type == AIDecisionType.Heal
                || type == AIDecisionType.Buff
                || type == AIDecisionType.Debuff
                || type == AIDecisionType.Summon;
        }

        private static bool CanMove(AIContext context)
        {
            return context?.Fighter != null
                && context.CurrentMP > 0
                && context.Fighter.CanBeMoved();
        }

        private static bool HasUsableContext(AIContext context)
        {
            return context?.Fighter != null
                && context.Fight != null
                && context.Fighter.Team != null
                && context.Fighter.Cell != null
                && !context.Fighter.IsFighterDead;
        }
    }
}
