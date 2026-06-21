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

        // Presupuesto del turno: persiste a lo largo de toda la planificacion iterativa para acotar
        // el numero total de acciones / hechizos / movimientos y garantizar que el turno termina.
        private AITurnBudget m_turnBudget;

        // Mientras esta activo se sigue re-planificando cuando se agota la cadena de acciones.
        private bool m_turnActive;

        public AIFighter Fighter { get; private set; }

        public AIActionBase CurrentAction { get; protected set; }

        protected AIBrain(AIFighter fighter)
        {
            Fighter = fighter;
            m_memory = new AILastDecisionMemory();
        }

        public virtual void OnTurnStart()
        {
            // En lugar de planificar todo el turno por adelantado, se prepara el estado y se planifica
            // UNA accion cada vez: tras ejecutarla se vuelve a evaluar con el tablero ya actualizado
            // (objetivos muertos, nuevas posiciones, hechizos recien habilitados...).
            m_turnActive = true;
            m_memory.BeginTurn();
            m_turnBudget = new AITurnBudget();
            CurrentAction = new DelayAIAction(Fighter, WorldConfig.FIGHT_AI_START_DELAY);
        }

        public virtual void OnUpdate()
        {
            if (CurrentAction == null)
            {
                // Cadena agotada: se re-planifica el siguiente paso (o se cierra el turno).
                if (m_turnActive)
                    CurrentAction = PlanNextStep();
                return;
            }

            CurrentAction.Update();
            if (CurrentAction.IsFinished)
                CurrentAction = CurrentAction.NextAction;
        }

        // Construye el siguiente paso del turno reevaluando el estado actual.
        private AIActionBase PlanNextStep()
        {
            try
            {
                if (!CanThink() || m_turnBudget == null || !m_turnBudget.CanContinue)
                    return EndTurnStep("Limite de turno o luchador no puede jugar");

                var context = new AIContext(Fighter, m_memory) { Budget = m_turnBudget };

                var decision = SelectNextDecision(context);
                if (decision == null || decision.Type == AIDecisionType.EndTurn)
                    return EndTurnStep(decision?.Reason ?? "Sin decision valida");

                context.CurrentPhase = DecisionTypeToPhase(decision.Type);
                LogDecision(context, decision);

                return BuildStep(context, decision);
            }
            catch (Exception ex)
            {
                if (WorldConfig.LOG_DEBUG)
                    Logger.Debug($"[IA] Luchador={(Fighter?.Id ?? 0)} fallo al planificar: {ex}");

                return EndTurnStep("Fallo al planificar IA");
            }
        }

        // Selecciona la mejor decision que quepa en el presupuesto/PA actuales.
        private AIDecision SelectNextDecision(AIContext context)
        {
            foreach (var decision in RankDecisions(context))
            {
                if (decision.Type == AIDecisionType.EndTurn)
                    return decision;

                if (CanSchedule(context, decision))
                    return decision;
            }

            return null;
        }

        // Evalua, aplica el bono de continuidad, deduplica y ordena por prioridad/score.
        private List<AIDecision> RankDecisions(AIContext context)
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
                .ToList();
        }

        // Una decision es planificable si cabe en el presupuesto del turno y, si es un hechizo, en el PA.
        private bool CanSchedule(AIContext context, AIDecision decision)
        {
            var availableAP = context.Fighter?.AP ?? 0;

            if (decision.Type == AIDecisionType.MoveAndCast)
            {
                return m_turnBudget.CanUse(AIDecisionType.Move)
                    && m_turnBudget.CanUse(AIDecisionType.CastSpell)
                    && GetApCost(context, decision) <= availableAP;
            }

            if (!m_turnBudget.CanUse(decision.Type))
                return false;

            if (AITurnBudget.IsSpellDecision(decision.Type))
                return GetApCost(context, decision) <= availableAP;

            return true;
        }

        // Crea la(s) accion(es) de un paso y consume el presupuesto correspondiente.
        private AIActionBase BuildStep(AIContext context, AIDecision decision)
        {
            if (decision.Type == AIDecisionType.MoveAndCast)
                return BuildMoveAndCastStep(context, decision);

            m_turnBudget.UseAction(decision.Type);
            m_memory.TrackUsage(decision.Type);
            m_memory.Record(decision);

            var head = new DecisionAIAction(Fighter, context, decision);
            head.LinkWith(new DelayAIAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            return head;
        }

        // Decision atomica mover-y-lanzar: desplazamiento, breve espera y lanzamiento encadenados.
        private AIActionBase BuildMoveAndCastStep(AIContext context, AIDecision decision)
        {
            if (decision.MoveCellId == null || decision.SpellId == null || decision.CellId == null)
                return EndTurnStep("MoveAndCast invalido");

            var moveDecision = AIDecision.Move(decision.MoveCellId.Value, decision.Score, decision.Priority, decision.Reason);
            var castDecision = AIDecision.CastSpell(decision.SpellId.Value, decision.CellId.Value, decision.TargetId ?? 0, decision.Score, decision.Priority, decision.Reason);

            m_turnBudget.UseAction(AIDecisionType.Move);
            m_turnBudget.UseAction(AIDecisionType.CastSpell);
            m_memory.TrackUsage(AIDecisionType.Move);
            m_memory.TrackUsage(AIDecisionType.CastSpell);
            m_memory.Record(castDecision);

            var head = new DecisionAIAction(Fighter, context, moveDecision);
            var tail = head.LinkWith(new DelayAIAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            tail = tail.LinkWith(new DecisionAIAction(Fighter, context, castDecision));
            tail.LinkWith(new DelayAIAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            return head;
        }

        // Cierra el turno: deja de re-planificar y encadena la accion de fin de turno.
        private AIActionBase EndTurnStep(string reason)
        {
            m_turnActive = false;
            return new DecisionAIAction(Fighter, null, AIDecision.EndTurn(reason));
        }

        protected abstract IEnumerable<AIDecision> Evaluate(AIContext context);

        protected virtual IEnumerable<AIDecision> GetFallbackDecisions(AIContext context)
        {
            yield return AIDecision.EndTurn("Fin de turno de respaldo");
        }

        protected virtual void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
                return;

            Logger.Debug($"[IA] Luchador={(Fighter?.Id ?? 0)} Fase={(context?.CurrentPhase.ToString() ?? "?")} Decision={decision.Type} Prioridad={decision.Priority} Puntuacion={decision.Score} Motivo={decision.Reason}");
        }

        private static AITurnPhase DecisionTypeToPhase(AIDecisionType type)
        {
            switch (type)
            {
                case AIDecisionType.Heal: return AITurnPhase.Heal;
                case AIDecisionType.Summon: return AITurnPhase.Summon;
                case AIDecisionType.Buff: return AITurnPhase.Buff;
                case AIDecisionType.Debuff: return AITurnPhase.Debuff;
                case AIDecisionType.CastSpell: return AITurnPhase.Attack;
                case AIDecisionType.MoveAndCast: return AITurnPhase.Move;
                case AIDecisionType.Move: return AITurnPhase.Move;
                case AIDecisionType.EndTurn: return AITurnPhase.End;
                default: return AITurnPhase.Start;
            }
        }

        private static int GetApCost(AIContext context, AIDecision decision)
        {
            if (decision?.SpellId == null)
                return 0;

            return context.Fighter?.SpellBook?.GetSpellLevel(decision.SpellId.Value)?.APCost ?? 0;
        }

        private static string GetDecisionKey(AIDecision decision)
        {
            if (decision == null)
                return string.Empty;

            return ((int)decision.Type) + ":"
                + (decision.SpellId?.ToString() ?? string.Empty) + ":"
                + (decision.TargetId?.ToString() ?? string.Empty) + ":"
                + (decision.CellId?.ToString() ?? string.Empty) + ":"
                + (decision.MoveCellId?.ToString() ?? string.Empty);
        }

        private bool CanThink()
        {
            return Fighter != null && Fighter.Fight != null && Fighter.Team != null && Fighter.Cell != null && !Fighter.IsFighterDead && Fighter.Fight.CurrentFighter == Fighter;
        }
    }
}
