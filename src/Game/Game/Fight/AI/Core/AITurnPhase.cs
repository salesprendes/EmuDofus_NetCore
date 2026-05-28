namespace Game.Fight.AI.Core
{
    /// <summary>
    /// Fase semántica del turno del AI.
    ///
    /// Se expone en <see cref="AIContext.CurrentPhase"/> y se actualiza en
    /// <see cref="AIBrain"/> antes de vincular cada decisión.  Permite que:
    ///
    ///   • Los evaluadores de boss comprueben en qué fase del turno se está
    ///     para escalar prioridades o inhibir acciones.
    ///   • El logger muestre la fase junto con cada decisión.
    ///   • Los cerebros especializados (<c>KralamarBrain</c>, etc.) establezcan
    ///     explícitamente <c>CriticalMechanic</c> para sus mecánicas de boss.
    ///
    /// Orden natural de prioridad:
    ///   Start → CriticalMechanic → Heal → Summon → Buff → Debuff → Attack → Move → Fallback → End
    /// </summary>
    public enum AITurnPhase
    {
        /// <summary>Inicio del turno, antes de cualquier evaluación.</summary>
        Start,

        /// <summary>
        /// Mecánicas críticas de boss (invocaciones obligatorias, cambios de fase…).
        /// Solo los cerebros de boss lo establecen explícitamente.
        /// </summary>
        CriticalMechanic,

        /// <summary>Curación de aliados.</summary>
        Heal,

        /// <summary>Invocación de aliados.</summary>
        Summon,

        /// <summary>Aplicación de buffs propios o de aliados.</summary>
        Buff,

        /// <summary>Aplicación de debuffs a enemigos (quitar PA/PM…).</summary>
        Debuff,

        /// <summary>Ataque / lanzamiento de hechizo dañino.</summary>
        Attack,

        /// <summary>Movimiento táctico.</summary>
        Move,

        /// <summary>Fase de reserva: ninguna decisión válida encontrada.</summary>
        Fallback,

        /// <summary>Final del turno.</summary>
        End
    }
}
