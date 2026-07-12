using Game.Map;
using System.Collections.Generic;

namespace Game.Fight.AI.Cache
{
    /// <summary>
    /// Línea de visión de la IA. NO tiene algoritmo propio: delega siempre en
    /// <see cref="Pathfinding.CheckView"/>, el mismo port del cliente 1.29 que valida los
    /// lanzamientos de los jugadores, de modo que la IA no puede "ver" ni más ni menos que ellos.
    /// Solo añade memorización dentro del paso de planificación actual.
    /// </summary>
    public sealed class AILineOfSightCache(AbstractFighter owner)
    {
        private readonly Dictionary<(int From, int To, int Self), bool> m_cache = new Dictionary<(int, int, int), bool>();

        /// <param name="selfCell">
        /// Celda que ocupará la IA al ejecutar lo que está evaluando (la de origen si va a lanzar
        /// tras moverse, la de destino si está midiendo quién la vería desde allí). Por defecto, su
        /// celda actual: entonces el tablero proyectado coincide con el real.
        /// </param>
        public bool HasLineOfSight(int fromCell, int toCell, int selfCell = -1)
        {
            var fight = owner?.Fight;
            if (fight == null || fromCell < 0 || toCell < 0)
                return false;

            if (fromCell == toCell)
                return true;

            if (selfCell < 0)
                selfCell = owner.Cell?.Id ?? -1;

            // La clave conserva el sentido de la mirada: CheckView no es simétrico (la altura del
            // ojo del que mira y la del objetivo entran en la interpolación de alturas), así que
            // meter (A,B) y (B,A) en la misma entrada devolvía la vista del sentido contrario.
            var key = (fromCell, toCell, selfCell);
            if (m_cache.TryGetValue(key, out bool result))
                return result;

            try
            {
                result = Pathfinding.CheckView(fight, fromCell, toCell, owner, selfCell);
            }
            catch (System.Exception ex)
            {
                // Sin algoritmo de reserva: uno alternativo volvería a separar la vista de la IA de
                // la del jugador, que es justo lo que hay que evitar. Se deniega la vista y se avisa.
                AIDiagnostics.LogSwallowed("AILineOfSightCache.HasLineOfSight", ex);
                result = false;
            }

            m_cache[key] = result;
            return result;
        }
    }
}
