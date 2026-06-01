using Game.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Cache
{
    public sealed class AICellCache
    {
        private readonly AIFighter m_fighter;
        private readonly Dictionary<long, int> m_distances;
        private readonly Dictionary<int, List<int>> m_neighbors;
        private readonly Dictionary<int, bool> m_walkable;
        private readonly Dictionary<int, string> m_paths;
        private List<int> m_reachableCells;

        public AICellCache(AIFighter fighter)
        {
            m_fighter = fighter;
            m_distances = new Dictionary<long, int>();
            m_neighbors = new Dictionary<int, List<int>>();
            m_walkable = new Dictionary<int, bool>();
            m_paths = new Dictionary<int, string>();
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
            catch
            {
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

            cells = CellZone.GetAdjacentCells(map, cellId)
                .Where(c => c >= 0 && m_fighter.Fight.GetCell(c) != null)
                .ToList();
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
            m_reachableCells.Add(startCell);

            foreach (var cellId in CellZone.GetCircleCells(m_fighter.Fight.Map, startCell, m_fighter.MP))
            {
                if (cellId == startCell || !IsCellFree(cellId))
                {
                    continue;
                }

                var path = GetPathToCell(cellId);
                if (!string.IsNullOrEmpty(path))
                {
                    m_reachableCells.Add(cellId);
                }
            }

            return m_reachableCells;
        }

        public string GetPathToCell(int targetCell)
        {
            string path;
            if (m_paths.TryGetValue(targetCell, out path))
            {
                return path;
            }

            path = string.Empty;

            try
            {
                if (m_fighter?.Fight?.Map?.Pathmaker != null && m_fighter.Cell != null && m_fighter.MP > 0 && m_fighter.CanBeMoved())
                {
                    path = m_fighter.Fight.Map.Pathmaker.FindPathAsString(m_fighter.Cell.Id, targetCell, false, m_fighter.MP, m_fighter.Fight.Obstacles);

                    // Validate the path in the fight context (handles stop cells, fighters on cells, etc.).
                    // If IsValidPath returns null OR MovementLength == 0 (fighter immediately blocked —
                    // e.g. the target cell is occupied by an enemy), the path is considered unusable.
                    var validatedPath = string.IsNullOrEmpty(path)
                        ? null
                        : Pathfinding.IsValidPath(m_fighter.Fight, m_fighter, m_fighter.Cell.Id, path);

                    if (validatedPath == null || validatedPath.MovementLength <= 0)
                    {
                        path = string.Empty;
                    }
                }
            }
            catch
            {
                path = string.Empty;
            }

            m_paths[targetCell] = path;
            return path;
        }

        private static long BuildPairKey(int first, int second)
        {
            var min = Math.Min(first, second);
            var max = Math.Max(first, second);
            return ((long)min << 32) | (uint)max;
        }
    }
}
