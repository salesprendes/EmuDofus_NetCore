using Game.Map;
using System.Collections.Generic;

namespace Game.Fight.AI.Cache
{
    public sealed class AILineOfSightCache(AbstractFight fight)
    {
        private readonly Dictionary<long, bool> m_cache = new Dictionary<long, bool>();

        public bool HasLineOfSight(int fromCell, int toCell)
        {
            if (fight == null || fromCell < 0 || toCell < 0)
                return false;

            if (fromCell == toCell)
                return true;

            long key = fromCell < toCell ? ((long)fromCell << 32) | (uint)toCell : ((long)toCell << 32) | (uint)fromCell;

            if (m_cache.TryGetValue(key, out bool result))
                return result;

            try
            {
                result = Pathfinding.CheckView(fight, fromCell, toCell);
            }
            catch
            {
                result = MapLos(fight?.Map, fromCell, toCell);
            }

            m_cache[key] = result;
            return result;
        }


        private static bool MapLos(MapInstance map, int fromCell, int toCell)
        {
            if (map == null || fromCell == toCell) return fromCell == toCell;

            var p0 = Pathfinding.GetPoint(map, fromCell);
            var p1 = Pathfinding.GetPoint(map, toCell);
            if (p0.X < 0 || p1.X < 0) return false;

            int x0 = (int)p0.X, y0 = (int)p0.Y;
            int x1 = (int)p1.X, y1 = (int)p1.Y;

            bool steep = System.Math.Abs(y1 - y0) > System.Math.Abs(x1 - x0);
            if (steep) { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }
            if (x0 > x1) { Swap(ref x0, ref x1); Swap(ref y0, ref y1); }

            int dx = x1 - x0, dy = System.Math.Abs(y1 - y0);
            int err = 0, y = y0, ystep = y0 < y1 ? 1 : -1;

            for (int x = x0; x <= x1; x++)
            {
                int cellId = steep ? Pathfinding.GetCell1(map, y, x) : Pathfinding.GetCell1(map, x, y);

                if (cellId != fromCell && cellId != toCell)
                {
                    var cell = map.GetCell(cellId);
                    if (cell == null || !cell.LineOfSight) return false;
                }

                err += dy;
                if (2 * err >= dx) { y += ystep; err -= dx; }
            }

            return true;
        }

        private static void Swap(ref int a, ref int b) => (b, a) = (a, b);
    }
}
