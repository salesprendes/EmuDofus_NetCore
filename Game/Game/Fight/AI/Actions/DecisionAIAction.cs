using Game.Fight.AI.Core;

namespace Game.Fight.AI.Actions
{
    public sealed class DecisionAIAction : AIActionBase
    {
        private readonly AIDecision m_decision;
        private readonly string m_failureKey;
        private AIContext m_context;
        private IAIAction m_action;
        private bool m_executed;

        public DecisionAIAction(AIFighter fighter, AIContext context, AIDecision decision, string failureKey = null)
            : base(fighter)
        {
            m_context = context;
            m_decision = decision ?? AIDecision.EndTurn("Decision nula");
            m_failureKey = failureKey;
        }


        protected override ChainResult OnInitialize()
        {
            m_context = m_context ?? new AIContext(Fighter);
            m_action = CreateAction(m_decision);

            if (m_action == null)
            {
                RegisterFailure();
                return ChainResult.Done;
            }

            if (!m_action.CanExecute(m_context))
            {
                RegisterFailure();
                return ChainResult.Done;
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
                    RegisterFailure();
                    return ChainResult.Done;
                }


                if (result.ShouldEndTurn || result.DelayMs <= 0)
                    return ChainResult.Done;


                Timeout = result.DelayMs;
                return ChainResult.Running;
            }

            return Timedout ? ChainResult.Done : ChainResult.Running;
        }

        // Ademas de consumir presupuesto de fallos, veta la decision para el resto del turno:
        // re-planificar la misma opcion fallida quemaba el turno sin intentar alternativas.
        private void RegisterFailure()
        {
            m_context?.Budget?.FailAction();

            if (!string.IsNullOrEmpty(m_failureKey))
                m_context?.FailedDecisionKeys?.Add(m_failureKey);
        }


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
                    return new EndTurnAIAction(AIDecision.EndTurn("Tipo de decision desconocido"));
            }
        }
    }
}
