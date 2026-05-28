using Game.Fight.AI.Core;

namespace Game.Fight.AI.Actions
{
    /// <summary>
    /// Ejecuta una <see cref="AIDecision"/> dentro de la cadena de acciones del AI.
    ///
    /// Hereda de <see cref="AIActionBase"/> (nueva capa) — ya no depende del legacy
    /// <c>AI.Action.AIAction</c>.
    ///
    /// Flujo:
    ///   OnInitialize → crea la <see cref="IAIAction"/> concreta vía <see cref="CreateAction"/>
    ///                  y valida que puede ejecutarse.
    ///   OnExecute    → delega en <see cref="IAIAction.Execute"/> y espera el delay estimado.
    /// </summary>
    public sealed class DecisionAIAction : AIActionBase
    {
        private readonly AIDecision m_decision;
        private AIContext m_context;
        private IAIAction m_action;
        private bool m_executed;

        public DecisionAIAction(AIFighter fighter, AIContext context, AIDecision decision)
            : base(fighter)
        {
            m_context = context;
            m_decision = decision ?? AIDecision.EndTurn("Null decision");
        }

        // ─────────────────────────────────────────────────────────────────────────
        protected override ChainResult OnInitialize()
        {
            m_context = m_context ?? new AIContext(Fighter);
            m_action = CreateAction(m_decision);

            if (m_action == null)
            {
                m_context?.Budget?.FailAction();
                return ChainResult.Done; // acción desconocida — saltar
            }

            if (!m_action.CanExecute(m_context))
            {
                m_context?.Budget?.FailAction();
                return ChainResult.Done; // condiciones no válidas — saltar
            }

            return ChainResult.Running;
        }

        protected override ChainResult OnExecute()
        {
            if (!m_executed)
            {
                var result = m_action.Execute(m_context);
                m_executed = true;

                if (result == null || !result.Success)
                {
                    m_context?.Budget?.FailAction();
                    return ChainResult.Done;
                }

                // EndTurn o sin delay → terminar inmediatamente
                if (result.ShouldEndTurn || result.DelayMs <= 0)
                    return ChainResult.Done;

                // Esperar el tiempo estimado de la acción (animación del hechizo, movimiento…)
                Timeout = result.DelayMs;
                return ChainResult.Running;
            }

            return Timedout ? ChainResult.Done : ChainResult.Running;
        }

        // ─── Factory ──────────────────────────────────────────────────────────────
        private static IAIAction CreateAction(AIDecision decision)
        {
            switch (decision.Type)
            {
                case AIDecisionType.CastSpell:
                case AIDecisionType.Debuff:
                    return new CastSpellAIAction(decision);

                case AIDecisionType.Move:
                    return new MoveToCellAIAction(decision);

                case AIDecisionType.Summon:
                    return new SummonAIAction(decision);

                case AIDecisionType.Buff:
                    return new BuffAIAction(decision);

                case AIDecisionType.Heal:
                    return new HealAIAction(decision);

                case AIDecisionType.EndTurn:
                    return new EndTurnAIAction(decision);

                default:
                    return new EndTurnAIAction(AIDecision.EndTurn("Unknown decision type"));
            }
        }
    }
}
