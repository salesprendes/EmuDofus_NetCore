using Game.Fight;

namespace Game.Fight.AI.Core
{
    /// <summary>
    /// Información pre-calculada sobre un objetivo enemigo para el turno actual.
    ///
    /// Se construye una vez por turno en <see cref="AIContext"/> y se reutiliza en
    /// todos los evaluadores, evitando recalcular distancias y visibilidad.
    ///
    /// Garantías:
    ///   • <see cref="Distance"/> usa la cache de celdas — O(1) tras el primer cálculo.
    ///   • La lista en <see cref="AIContext.EnemyTargets"/> está ordenada por distancia
    ///     ascendente, lista para iterar sin Sort adicional.
    /// </summary>
    public sealed class AITargetInfo
    {
        /// <summary>Referencia al combatiente enemigo.</summary>
        public AbstractFighter Target { get; }

        /// <summary>
        /// true si el objetivo está visible desde la celda actual del AI.
        /// false cuando está oculto/invisible.
        /// TODO: conectar con la API de invisibilidad del motor cuando esté expuesta.
        ///       Por ahora siempre true (visibilidad conservadora).
        /// </summary>
        public bool IsVisible { get; }

        /// <summary>ID de celda conocido del objetivo (−1 si desconocido).</summary>
        public int CellId { get; }

        /// <summary>
        /// Distancia de Chebyshev (GoalDistance) desde la celda del AI a la del objetivo.
        /// Calculada a través de <see cref="Cache.AICellCache.GetDistance"/> para aprovechar
        /// el caché de la cache de celdas del turno.
        /// </summary>
        public int Distance { get; }

        public AITargetInfo(AbstractFighter target, int distance)
        {
            Target     = target;
            Distance   = distance;
            IsVisible  = true; // TODO: invisibilidad futura
            CellId     = target?.Cell?.Id ?? -1;
        }
    }
}
