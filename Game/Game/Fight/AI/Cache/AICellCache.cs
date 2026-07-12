using System;
using System.Collections.Generic;
using System.Linq;
using Game.Map;

namespace Game.Fight.AI.Cache
{
    public sealed class AICellCache
    {
        private readonly AIFighter m_fighter;
        private readonly Dictionary<long, int> m_distances;
        private readonly Dictionary<int, List<int>> m_neighbors;
        private readonly Dictionary<int, bool> m_walkable;
        private readonly Dictionary<int, string> m_paths;
        private readonly Dictionary<int, string> m_approachPaths;
        private readonly Dictionary<int, Dictionary<int, int>> m_approachDistances;
        private List<int> m_reachableCells;
        private HashSet<int> m_stopCells;

        public AICellCache(AIFighter fighter)
        {
            m_fighter = fighter;
            m_distances = new Dictionary<long, int>();
            m_neighbors = new Dictionary<int, List<int>>();
            m_walkable = new Dictionary<int, bool>();
            m_paths = new Dictionary<int, string>();
            m_approachPaths = new Dictionary<int, string>();
            m_approachDistances = new Dictionary<int, Dictionary<int, int>>();
        }

        public int GetDistance(int fromCell, int toCell)
        {
            var map = m_fighter?.Fight?.Map;
            if (map == null || fromCell < 0 || toCell < 0)
            {
                return int.MaxValue;
            }

            var key = BuildPairKey(fromCell, toCell);
            int distance;
            if (m_distances.TryGetValue(key, out distance))
            {
                return distance;
            }

            try
            {
                distance = Pathfinding.GoalDistance(map, fromCell, toCell);
            }
            catch (Exception ex)
            {
                AIDiagnostics.LogSwallowed("AICellCache.GetDistance", ex);
                distance = int.MaxValue;
            }

            m_distances[key] = distance;
            return distance;
        }

        public IReadOnlyList<int> GetNeighbors(int cellId)
        {
            List<int> cells;
            if (m_neighbors.TryGetValue(cellId, out cells))
            {
                return cells;
            }

            var map = m_fighter?.Fight?.Map;
            if (map == null)
            {
                return new List<int>();
            }

            cells = CellZone.GetAdjacentCells(map, cellId).Where(c => c >= 0 && m_fighter.Fight.GetCell(c) != null).ToList();
            m_neighbors[cellId] = cells;
            return cells;
        }

        public bool IsCellFree(int cellId)
        {
            bool walkable;
            if (m_walkable.TryGetValue(cellId, out walkable))
            {
                return walkable;
            }

            var fightCell = m_fighter?.Fight?.GetCell(cellId);
            walkable = fightCell != null && fightCell.CanWalk;
            m_walkable[cellId] = walkable;
            return walkable;
        }

        /// <summary>
        /// Celdas que cortan el movimiento en el motor (IsStopCell): las adyacentes a un enemigo
        /// vivo. Un camino que pisa una de ellas termina ahi, asi que la IA debe planificar con
        /// la misma regla o creera alcanzable lo que no lo es.
        /// </summary>
        public IReadOnlySet<int> GetStopCells()
        {
            if (m_stopCells != null)
            {
                return m_stopCells;
            }

            m_stopCells = new HashSet<int>();

            var fight = m_fighter?.Fight;
            var enemies = m_fighter?.Team?.OpponentTeam?.AliveFighters;
            if (fight?.Map == null || enemies == null)
            {
                return m_stopCells;
            }

            foreach (var enemy in enemies)
            {
                if (enemy?.Cell == null || enemy.IsFighterDead)
                {
                    continue;
                }

                foreach (var neighbor in GetNeighbors(enemy.Cell.Id))
                {
                    m_stopCells.Add(neighbor);
                }
            }

            return m_stopCells;
        }

        public IReadOnlyList<int> GetReachableCells()
        {
            if (m_reachableCells != null)
            {
                return m_reachableCells;
            }

            m_reachableCells = new List<int>();

            if (m_fighter?.Fight?.Map == null || m_fighter.Cell == null || m_fighter.MP <= 0 || !m_fighter.CanBeMoved())
            {
                if (m_fighter?.Cell != null)
                {
                    m_reachableCells.Add(m_fighter.Cell.Id);
                }

                return m_reachableCells;
            }

            var startCell = m_fighter.Cell.Id;
            var maxMP = m_fighter.MP;
            var stopCells = GetStopCells();

            // BFS (flood-fill) limitado a los PM en una sola pasada, en vez de un pathfinding por
            // cada casilla del circulo. Como el movimiento de combate es a 4 vecinos con coste 1,
            // alcanzar una casilla por BFS sobre celdas transitables equivale al pathfinding.
            var distance = new Dictionary<int, int> { [startCell] = 0 };
            var frontier = new Queue<int>();
            frontier.Enqueue(startCell);
            m_reachableCells.Add(startCell);

            while (frontier.Count > 0)
            {
                var cell = frontier.Dequeue();
                var cost = distance[cell];
                if (cost >= maxMP)
                {
                    continue;
                }

                // Una stop cell (adyacente a un enemigo) o una celda con trampa es alcanzable pero
                // el motor detiene el movimiento en ella: no se expande mas alla. Sin la trampa
                // aqui, la IA creia alcanzables celdas al otro lado y, al truncarse el camino en la
                // trampa, descartaba el movimiento y pasaba turno.
                if (cell != startCell && (stopCells.Contains(cell) || CellHasTrap(cell)))
                {
                    continue;
                }

                foreach (var neighbor in GetNeighbors(cell))
                {
                    if (distance.ContainsKey(neighbor) || !IsCellFree(neighbor))
                    {
                        continue;
                    }

                    // Trampa detectada: muro. Ni se pisa ni se cruza; el monstruo la rodea.
                    if (IsAvoidedTrap(neighbor))
                    {
                        continue;
                    }

                    distance[neighbor] = cost + 1;
                    m_reachableCells.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }

            return m_reachableCells;
        }

        /// <summary>
        /// Distancias reales de marcha (BFS sobre celdas transitables, sin limite de PM) desde
        /// targetCell hasta cada celda del mapa. A diferencia de la distancia Manhattan, rodea
        /// muros y luchadores: evita que la IA se quede "pegada a la pared" persiguiendo la
        /// distancia en linea recta. La propia targetCell (ocupada por el enemigo) es el origen.
        /// </summary>
        public IReadOnlyDictionary<int, int> GetApproachDistances(int targetCell)
        {
            if (m_approachDistances.TryGetValue(targetCell, out var cached))
            {
                return cached;
            }

            var distances = new Dictionary<int, int> { [targetCell] = 0 };
            m_approachDistances[targetCell] = distances;

            if (m_fighter?.Fight?.Map == null || targetCell < 0)
            {
                return distances;
            }

            var frontier = new Queue<int>();
            frontier.Enqueue(targetCell);

            while (frontier.Count > 0)
            {
                var cell = frontier.Dequeue();
                var cost = distances[cell];

                foreach (var neighbor in GetNeighbors(cell))
                {
                    if (distances.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    // La celda del propio luchador cuenta como transitable: esta "ocupada" por
                    // el mismo y no debe cortar su propia ruta de aproximacion.
                    if (!IsCellFree(neighbor) && neighbor != (m_fighter.Cell?.Id ?? -1))
                    {
                        continue;
                    }

                    distances[neighbor] = cost + 1;
                    frontier.Enqueue(neighbor);
                }
            }

            return distances;
        }

        /// <summary>
        /// Construye un camino que termina EXACTAMENTE en targetCell, planificando con las mismas
        /// reglas que aplicara el motor: los obstaculos del combate y las stop cells no son
        /// transitables (el A* exime la celda destino, por lo que si puede terminar pegado a un
        /// enemigo). Devuelve null si no existe tal camino con los PM actuales: el Pathmaker por
        /// si solo nunca falla (devuelve rutas parciales "hacia" el destino) y aceptar esas rutas
        /// es lo que hacia que los monstruos acabaran en celdas no planificadas.
        /// </summary>
        public string GetExactPathToCell(int targetCell)
        {
            string path;
            if (m_paths.TryGetValue(targetCell, out path))
            {
                return path;
            }

            path = null;

            try
            {
                if (m_fighter?.Fight?.Map?.Pathmaker != null && m_fighter.Cell != null && m_fighter.MP > 0 && m_fighter.CanBeMoved())
                {
                    var obstacles = BuildPathObstacles(targetCell);
                    var candidate = m_fighter.Fight.Map.Pathmaker.FindPathAsString(m_fighter.Cell.Id, targetCell, false, m_fighter.MP, obstacles);

                    var validatedPath = string.IsNullOrEmpty(candidate) ? null : Pathfinding.IsValidPath(m_fighter.Fight, m_fighter, m_fighter.Cell.Id, candidate);

                    if (validatedPath != null
                        && validatedPath.MovementLength > 0
                        && validatedPath.MovementLength <= m_fighter.MP
                        && validatedPath.EndCell == targetCell)
                    {
                        path = candidate;
                    }
                }
            }
            catch (Exception ex)
            {
                AIDiagnostics.LogSwallowed("AICellCache.GetExactPathToCell", ex);
                path = null;
            }

            m_paths[targetCell] = path;
            return path;
        }

        /// <summary>
        /// Camino de aproximacion al objetivo aceptando que el motor lo trunque en una stop cell
        /// (trampa oculta o casilla pegada a un enemigo): en ese caso el luchador avanza hasta
        /// donde puede y PISA la trampa, en vez de descartar el movimiento y pasar turno. Devuelve
        /// null si no hay avance o si el corte se debe a un muro (para no derivar a celdas no
        /// planificadas, la razon por la que existe GetExactPathToCell).
        /// </summary>
        public string GetApproachPathToCell(int targetCell)
        {
            if (m_approachPaths.TryGetValue(targetCell, out var cached))
            {
                return cached;
            }

            string path = null;

            try
            {
                if (m_fighter?.Fight?.Map?.Pathmaker != null && m_fighter.Cell != null && m_fighter.MP > 0 && m_fighter.CanBeMoved())
                {
                    var obstacles = BuildPathObstacles(targetCell);
                    var candidate = m_fighter.Fight.Map.Pathmaker.FindPathAsString(m_fighter.Cell.Id, targetCell, false, m_fighter.MP, obstacles);

                    var validated = string.IsNullOrEmpty(candidate) ? null : Pathfinding.IsValidPath(m_fighter.Fight, m_fighter, m_fighter.Cell.Id, candidate);

                    if (validated != null
                        && validated.MovementLength > 0
                        && validated.MovementLength <= m_fighter.MP
                        && validated.EndCell != m_fighter.Cell.Id
                        && (validated.EndCell == targetCell || Pathfinding.IsStopCell(m_fighter.Fight, m_fighter.Team, validated.EndCell)))
                    {
                        path = candidate;
                    }
                }
            }
            catch (Exception ex)
            {
                AIDiagnostics.LogSwallowed("AICellCache.GetApproachPathToCell", ex);
                path = null;
            }

            m_approachPaths[targetCell] = path;
            return path;
        }

        private bool CellHasTrap(int cellId)
        {
            var cell = m_fighter?.Fight?.GetCell(cellId);
            return cell != null && cell.HasObject(FightObstacleTypeEnum.TYPE_TRAP);
        }

        private bool IsAvoidedTrap(int cellId)
        {
            if (!CellHasTrap(cellId))
                return false;

            var awareness = m_fighter?.TrapAvoidanceThisTurn;
            if (awareness == null)
                return false;

            if (awareness.TryGetValue(cellId, out var avoid))
                return avoid;

            avoid = Util.Next(0, 100) < 25;
            awareness[cellId] = avoid;
            return avoid;
        }

        private List<int> BuildPathObstacles(int targetCell)
        {
            var obstacles = new List<int>();

            var fightObstacles = m_fighter?.Fight?.Obstacles;
            if (fightObstacles != null)
            {
                obstacles.AddRange(fightObstacles);
            }

            foreach (var stopCell in GetStopCells())
            {
                if (stopCell != targetCell)
                {
                    obstacles.Add(stopCell);
                }
            }
            
            if (m_fighter?.TrapAvoidanceThisTurn != null)
            {
                foreach (var kv in m_fighter.TrapAvoidanceThisTurn)
                {
                    if (kv.Value && kv.Key != targetCell)
                    {
                        obstacles.Add(kv.Key);
                    }
                }
            }

            return obstacles;
        }

        private static long BuildPairKey(int first, int second)
        {
            var min = Math.Min(first, second);
            var max = Math.Max(first, second);
            return ((long)min << 32) | (uint)max;
        }
    }
}
