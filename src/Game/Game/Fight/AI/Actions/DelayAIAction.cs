using System;

namespace Game.Fight.AI.Actions
{
    /// <summary>
    /// Pausa de <paramref name="delay"/> milisegundos dentro de la cadena de acciones del AI.
    ///
    /// Reemplaza al antiguo <c>AI.Action.Type.DelayAction</c> que heredaba del legacy
    /// <c>AIAction</c>.  Ahora hereda de <see cref="AIActionBase"/> y vive en la capa
    /// nueva <c>AI.Actions</c>.
    ///
    /// Uso habitual en <see cref="Core.AIBrain.OnTurnStart"/>:
    /// <code>
    /// // Pausa inicial antes del primer pensamiento
    /// var startDelay = new DelayAIAction(Fighter, WorldConfig.FIGHT_AI_START_DELAY);
    ///
    /// // Pausa entre decisiones
    /// tail = tail.LinkWith(new DelayAIAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
    /// </code>
    /// </summary>
    public sealed class DelayAIAction : AIActionBase
    {
        private readonly int m_delay;

        /// <param name="fighter">Combatiente dueño del turno.</param>
        /// <param name="delay">Duración de la pausa en milisegundos. &lt;= 0 termina inmediatamente.</param>
        public DelayAIAction(AIFighter fighter, int delay)
            : base(fighter)
        {
            m_delay = Math.Max(0, delay);
        }

        protected override ChainResult OnInitialize()
        {
            if (m_delay == 0)
                return ChainResult.Done; // sin espera, terminar al instante

            Timeout = m_delay;
            return ChainResult.Running;
        }

        protected override ChainResult OnExecute()
        {
            return Timedout ? ChainResult.Done : ChainResult.Running;
        }
    }
}
