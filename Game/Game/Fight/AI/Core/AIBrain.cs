using Game.Fight.AI.Actions;
using Protocolo.Framework.Generic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Core
{
    public abstract class AIBrain
    {
        protected static readonly ILogger Logger = LogManager.GetLogger(typeof(AIBrain));
        private readonly AILastDecisionMemory m_memory;

        public AIFighter Fighter { get; private set; }

        /// <summary>
        /// Acción actualmente en ejecución dentro de la cadena del turno.
        /// <see cref="OnUpdate"/> avanza al siguiente eslabón cuando <see cref="AIActionBase.IsFinished"/>.
        /// </summary>
        public AIActionBase CurrentAction { get; protected set; }

        protected AIBrain(AIFighter fighter)
        {
            Fighter = fighter;
            m_memory = new AILastDecisionMemory();
        }

        public virtual void OnTurnStart()
        {
            var startDelay = new DelayAIAction(Fighter, WorldConfig.FIGHT_AI_START_DELAY);
            CurrentAction = startDelay;

            AIActionBase tail = startDelay;

            try
            {
                if (!CanThink())
                {
                    tail.LinkWith(new DecisionAIAction(Fighter, null, AIDecision.EndTurn("Fighter cannot play")));
                    return;
                }

                // Resetear el historial de uso por tipo del turno anterior
                m_memory.BeginTurn();

                var context = new AIContext(Fighter, m_memory);
                var decisions = SelectDecisions(context);

                if (decisions.Count == 0)
                    decisions.Add(AIDecision.EndTurn("No valid decision"));

                var linkedActions = 0;
                var queuedEndTurn = false;

                foreach (var decision in decisions)
                {
                    if (decision == null || !decision.IsValid)
                        continue;

                    if (!context.Budget.CanUse(decision.Type))
                        continue;

                    context.CurrentPhase = DecisionTypeToPhase(decision.Type);
                    LogDecision(context, decision);

                    tail = tail.LinkWith(new DecisionAIAction(Fighter, context, decision));
                    linkedActions++;
                    context.Budget.UseAction(decision.Type);
                    m_memory.TrackUsage(decision.Type);

                    if (decision.Type == AIDecisionType.EndTurn)
                    {
                        queuedEndTurn = true;
                        break;
                    }

                    m_memory.Record(decision);

                    if (!context.Budget.CanContinue)
                        break;

                    tail = tail.LinkWith(new DelayAIAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
                }

                if (linkedActions == 0 || !queuedEndTurn)
                    tail.LinkWith(new DecisionAIAction(Fighter, context, AIDecision.EndTurn("Turn budget complete")));
            }
            catch (Exception ex)
            {
                if (WorldConfig.LOG_DEBUG)
                    Logger.Debug("[AI] Fighter=" + (Fighter?.Id ?? 0) + " failed to evaluate: " + ex);

                tail.LinkWith(new DecisionAIAction(Fighter, null, AIDecision.EndTurn("AI evaluation failed")));
            }
        }

        public virtual void OnUpdate()
        {
            if (CurrentAction == null)
                return;

            CurrentAction.Update();
            if (CurrentAction.IsFinished)
                CurrentAction = CurrentAction.NextAction;
        }

        protected abstract IEnumerable<AIDecision> Evaluate(AIContext context);

        protected virtual IEnumerable<AIDecision> GetFallbackDecisions(AIContext context)
        {
            yield return AIDecision.EndTurn("Fallback end turn");
        }

        protected virtual void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
                return;

            Logger.Debug("[AI] Fighter=" + (Fighter?.Id ?? 0)
                + " Phase=" + (context?.CurrentPhase.ToString() ?? "?")
                + " Decision=" + decision.Type
                + " Priority=" + decision.Priority
                + " Score=" + decision.Score
                + " Reason=" + decision.Reason);
        }

        /// <summary>
        /// Mapea el tipo de decisión a la fase semántica del turno correspondiente.
        /// </summary>
        private static AITurnPhase DecisionTypeToPhase(AIDecisionType type)
        {
            switch (type)
            {
                case AIDecisionType.Heal:      return AITurnPhase.Heal;
                case AIDecisionType.Summon:    return AITurnPhase.Summon;
                case AIDecisionType.Buff:      return AITurnPhase.Buff;
                case AIDecisionType.Debuff:    return AITurnPhase.Debuff;
                case AIDecisionType.CastSpell: return AITurnPhase.Attack;
                case AIDecisionType.Move:      return AITurnPhase.Move;
                case AIDecisionType.EndTurn:   return AITurnPhase.End;
                default:                       return AITurnPhase.Start;
            }
        }

        private List<AIDecision> SelectDecisions(AIContext context)
        {
            var decisions = new List<AIDecision>();
            var evaluated = Evaluate(context);

            if (evaluated != null)
                decisions.AddRange(evaluated);

            if (decisions.Count == 0)
                decisions.AddRange(GetFallbackDecisions(context));

            foreach (var decision in decisions)
            {
                if (decision != null)
                    decision.Score += context.LastDecisionMemory.GetContinuityBonus(decision);
            }

            return decisions
                .Where(x => x != null && x.IsValid && x.Score > 0)
                .GroupBy(GetDecisionKey)
                .Select(g => g.OrderByDescending(x => x.Priority).ThenByDescending(x => x.Score).First())
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.Score)
                .Take(context.Budget.MaxActions)
                .ToList();
        }

        private static string GetDecisionKey(AIDecision decision)
        {
            if (decision == null)
                return string.Empty;

            return ((int)decision.Type) + ":"
                + (decision.SpellId?.ToString() ?? string.Empty) + ":"
                + (decision.TargetId?.ToString() ?? string.Empty) + ":"
                + (decision.CellId?.ToString() ?? string.Empty);
        }

        private bool CanThink()
        {
            return Fighter != null
                && Fighter.Fight != null
                && Fighter.Team != null
                && Fighter.Cell != null
                && !Fighter.IsFighterDead
                && Fighter.Fight.CurrentFighter == Fighter;
        }
    }
}
