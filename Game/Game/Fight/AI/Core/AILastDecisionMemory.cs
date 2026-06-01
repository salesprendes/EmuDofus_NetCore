using System.Collections.Generic;

namespace Game.Fight.AI.Core
{
    /// <summary>
    /// Memoria de decisiones del turno anterior para añadir continuidad (bonificación
    /// cuando el AI repite una decisión efectiva) y penalizar ciclos estériles
    /// (cuando repite el mismo movimiento sin resultado).
    ///   • Solo el Brain escribe en esta memoria (via Record).
    ///   • Se reutiliza entre turnos del mismo combatiente — persiste en AIBrain.
    ///   • Es inmutable durante el Evaluate del turno actual: se actualiza al final.
    /// </summary>
    public sealed class AILastDecisionMemory
    {
        // ─── Estado del turno anterior ────────────────────────────────────────────
        public int?  LastUsefulSpellId   { get; private set; }
        public long? LastTargetId        { get; private set; }
        public short? LastCellId         { get; private set; }
        public AIDecisionType? LastType  { get; private set; }

        // Contador de turnos seguidos usando la misma decisión (para detectar ciclos)
        public int ConsecutiveSameSpell  { get; private set; }
        public int ConsecutiveSameMove   { get; private set; }

        // Cuántas veces más puede repetir la misma decisión con bonus (evita spam infinito)
        private int m_retriesRemaining;

        // Historial de tipos de decisión del turno (para diversificación)
        private readonly Dictionary<AIDecisionType, int> m_typeUsageThisTurn = new Dictionary<AIDecisionType, int>();

        // ─── Constantes de ajuste ─────────────────────────────────────────────────
        private const int MaxRetries     = 2;   // Bonus se concede hasta 2 turnos seguidos
        private const int MaxSameSpell   = 4;   // Más de 4 repeticiones del mismo hechizo → penalizar
        private const int MaxSameMove    = 3;   // Más de 3 movimientos al mismo destino → penalizar

        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registra la decisión tomada al final del Evaluate del turno.
        /// Actualiza los contadores de continuidad/ciclo.
        /// </summary>
        public void Record(AIDecision decision)
        {
            if (decision == null || !decision.IsValid || decision.Type == AIDecisionType.EndTurn)
                return;

            bool sameSpell = decision.SpellId.HasValue && decision.SpellId == LastUsefulSpellId;
            bool sameCell  = decision.CellId.HasValue  && decision.CellId  == LastCellId;

            if (sameSpell)
                ConsecutiveSameSpell++;
            else
                ConsecutiveSameSpell = 0;

            if (decision.Type == AIDecisionType.Move && sameCell)
                ConsecutiveSameMove++;
            else if (decision.Type == AIDecisionType.Move)
                ConsecutiveSameMove = 0;

            LastUsefulSpellId = decision.SpellId;
            LastTargetId      = decision.TargetId;
            LastCellId        = decision.CellId;
            LastType          = decision.Type;
            m_retriesRemaining = MaxRetries;
        }

        /// <summary>
        /// Limpia el historial de uso de tipos del turno anterior.
        /// Debe llamarse al inicio de cada turno (antes del Evaluate).
        /// </summary>
        public void BeginTurn()
        {
            m_typeUsageThisTurn.Clear();
        }

        /// <summary>
        /// Registra que un tipo de decisión ya fue seleccionado este turno.
        /// Permite aplicar penalización por diversificación.
        /// </summary>
        public void TrackUsage(AIDecisionType type)
        {
            if (!m_typeUsageThisTurn.ContainsKey(type))
                m_typeUsageThisTurn[type] = 0;
            m_typeUsageThisTurn[type]++;
        }

        /// <summary>
        /// Calcula el bonus de continuidad para una decisión propuesta.
        ///
        /// Bonificación:   +10 si mismo hechizo, +10 si mismo objetivo, +4 si misma celda.
        /// Penalización:   -30 por turno extra de mismo hechizo (anti-ciclo),
        ///                 -40 por movimiento repetido al mismo destino.
        /// </summary>
        public int GetContinuityBonus(AIDecision decision)
        {
            if (decision == null || m_retriesRemaining <= 0)
                return 0;

            int bonus = 0;

            // Bonus de continuidad
            if (decision.SpellId.HasValue && decision.SpellId == LastUsefulSpellId)
                bonus += 10;
            if (decision.TargetId.HasValue && decision.TargetId == LastTargetId)
                bonus += 10;
            if (decision.CellId.HasValue && decision.CellId == LastCellId)
                bonus += 4;

            // Penalización anti-ciclo: mismo hechizo demasiadas veces seguidas
            if (ConsecutiveSameSpell > MaxSameSpell && decision.SpellId.HasValue && decision.SpellId == LastUsefulSpellId)
            {
                bonus -= 30 * (ConsecutiveSameSpell - MaxSameSpell);
            }

            // Penalización anti-ciclo: mismo movimiento demasiadas veces seguidas
            if (decision.Type == AIDecisionType.Move && ConsecutiveSameMove > MaxSameMove && decision.CellId == LastCellId)
            {
                bonus -= 40 * (ConsecutiveSameMove - MaxSameMove);
            }

            return bonus;
        }

        /// <summary>
        /// Devuelve cuántas veces se ha tomado una decisión de este tipo en el turno actual.
        /// Útil para evaluar diversificación dentro del mismo turno.
        /// </summary>
        public int GetUsageCountThisTurn(AIDecisionType type)
        {
            return m_typeUsageThisTurn.TryGetValue(type, out var count) ? count : 0;
        }
    }
}
