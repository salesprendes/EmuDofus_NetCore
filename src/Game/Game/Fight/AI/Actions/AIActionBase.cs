using Protocolo.Framework.Generic.Logging;
using System;

namespace Game.Fight.AI.Actions
{
    /// <summary>
    /// Base de la cadena de acciones del AI.  Reemplaza al antiguo <c>AI.Action.AIAction</c>.
    ///
    /// Máquina de estados interna:
    ///   Init → OnInitialize() devuelve <see cref="ChainResult.Running"/>  → Exec
    ///   Init → OnInitialize() devuelve <see cref="ChainResult.Done"/>     → Finish
    ///   Exec → OnExecute()   devuelve <see cref="ChainResult.Running"/>  → sigue en Exec
    ///   Exec → OnExecute()   devuelve <see cref="ChainResult.Done"/>     → Finish
    ///   Finish → OnFinish() llamado una sola vez → <see cref="IsFinished"/> = true
    ///
    /// Encadenamiento:
    ///   <see cref="LinkWith"/> vincula la acción siguiente y devuelve la nueva cola,
    ///   permitiendo escribir:
    ///   <code>tail = tail.LinkWith(new DecisionAIAction(...));</code>
    ///   <see cref="AIBrain"/> avanza a <see cref="NextAction"/> cuando <see cref="IsFinished"/>.
    /// </summary>
    public abstract class AIActionBase
    {
        protected static readonly ILogger Logger = LogManager.GetLogger(typeof(AIActionBase));

        // ─── Máquina de estados ────────────────────────────────────────────────────
        private enum Phase { Init, Exec, Finish }
        private Phase m_phase = Phase.Init;
        private bool m_finishCalled;

        // ─── Propiedades públicas ──────────────────────────────────────────────────
        public AIFighter Fighter { get; }
        public AIActionBase NextAction { get; private set; }

        /// <summary>true cuando la acción ha terminado y el cerebro debe avanzar a NextAction.</summary>
        public bool IsFinished => m_phase == Phase.Finish && m_finishCalled;

        // ─── Timeout ──────────────────────────────────────────────────────────────
        private long m_timeoutTick;

        /// <summary>
        /// Fija el timeout en milisegundos relativos al tick actual del combate.
        /// Usar en <see cref="OnInitialize"/> o <see cref="OnExecute"/>.
        /// </summary>
        protected long Timeout
        {
            set => m_timeoutTick = (Fighter?.Fight?.UpdateTime ?? 0) + Math.Max(0, value);
        }

        /// <summary>true cuando el tick actual del combate supera el timeout establecido.</summary>
        protected bool Timedout => Fighter?.Fight != null
            && Fighter.Fight.UpdateTime >= m_timeoutTick;

        // ─── Constructor ──────────────────────────────────────────────────────────
        protected AIActionBase(AIFighter fighter)
        {
            Fighter = fighter;
        }

        // ─── Cadena ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Enlaza <paramref name="next"/> como la siguiente acción de la cadena y lo devuelve,
        /// permitiendo construir la cadena como: <c>tail = tail.LinkWith(next);</c>
        /// </summary>
        public AIActionBase LinkWith(AIActionBase next)
        {
            NextAction = next;
            return next;
        }

        // ─── Bucle principal ──────────────────────────────────────────────────────
        /// <summary>Llamado por <see cref="Core.AIBrain.OnUpdate"/> cada tick del combate.</summary>
        public void Update()
        {
            switch (m_phase)
            {
                case Phase.Init:
                    m_phase = OnInitialize() == ChainResult.Running
                        ? Phase.Exec
                        : Phase.Finish;
                    break;

                case Phase.Exec:
                    if (OnExecute() != ChainResult.Running)
                        m_phase = Phase.Finish;
                    break;

                case Phase.Finish:
                    if (!m_finishCalled)
                    {
                        OnFinish();
                        m_finishCalled = true;
                    }
                    break;
            }
        }

        // ─── Hooks para subclases ─────────────────────────────────────────────────

        /// <summary>
        /// Fase de inicialización.  Se llama una vez.
        /// Devuelve <see cref="ChainResult.Done"/> para saltar directamente al fin
        /// (p. ej. la acción detecta que no puede ejecutarse).
        /// </summary>
        protected virtual ChainResult OnInitialize() => ChainResult.Running;

        /// <summary>
        /// Fase de ejecución.  Se llama cada tick mientras devuelva <see cref="ChainResult.Running"/>.
        /// </summary>
        protected abstract ChainResult OnExecute();

        /// <summary>Fase de limpieza.  Se llama una única vez justo antes de marcar <see cref="IsFinished"/>.</summary>
        protected virtual void OnFinish() { }

        // ─── Resultado de la cadena ───────────────────────────────────────────────
        /// <summary>Resultado de los hooks de la máquina de estados.</summary>
        protected enum ChainResult
        {
            /// <summary>La acción sigue en curso.</summary>
            Running,

            /// <summary>La acción ha terminado (con éxito o fallo — ambos finalizan).</summary>
            Done
        }
    }
}
