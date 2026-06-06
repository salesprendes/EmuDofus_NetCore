using System.Collections.Generic;

namespace Game.Map
{
    public sealed class CellZone
    {
        private const int MaxRadius = 63;

        private static readonly DirectionEnum[] CardinalDirections =
        {
            DirectionEnum.Este, DirectionEnum.Sur, DirectionEnum.Oeste, DirectionEnum.Norte
        };

        public static IEnumerable<int> GetAdjacentCells(MapInstance map, int cellId)
        {
            if (map == null) yield break;
            
            yield return Pathfinding.NextCell(map, cellId, DirectionEnum.Este);
            yield return Pathfinding.NextCell(map, cellId, DirectionEnum.Sur);
            yield return Pathfinding.NextCell(map, cellId, DirectionEnum.Oeste);
            yield return Pathfinding.NextCell(map, cellId, DirectionEnum.Norte);
        }

        public static IEnumerable<int> GetLineCells(MapInstance map, int cellId, DirectionEnum direction, int length)
        {
            if (map == null) yield break;
            yield return cellId;
            length = Clamp(length, 0, MaxRadius);
            for (int i = 1; i <= length; i++)
            {
                var next = Pathfinding.NextCell(map, cellId, direction, i);
                if (IsInBounds(map, next))
                    yield return next;
            }
        }

        public static IEnumerable<int> GetCircleCells(MapInstance map, int currentCell, int radius)
        {
            if (map == null)
                return new HashSet<int>();

            radius = Clamp(radius, 0, MaxRadius);

            var cells    = new HashSet<int> { currentCell };
            var frontier = new List<int>    { currentCell };

            for (int i = 0; i < radius; i++)
            {
                var next = new List<int>();
                foreach (var cell in frontier)
                {
                    foreach (var adj in GetAdjacentCells(map, cell))
                    {
                        if (IsInBounds(map, adj) && cells.Add(adj))
                            next.Add(adj);
                    }
                }
                frontier = next;
            }

            return cells;
        }

        public static IEnumerable<int> GetRingCells(MapInstance map, int currentCell, int radiusIn, int radiusOut)
        {
            if (map == null) yield break;
            radiusIn  = Clamp(radiusIn,  0, MaxRadius);
            radiusOut = Clamp(radiusOut, 0, MaxRadius);
            if (radiusIn > radiusOut) yield break;

            var inner = radiusIn > 0
                ? new HashSet<int>(GetCircleCells(map, currentCell, radiusIn - 1))
                : new HashSet<int>();

            foreach (var cell in GetCircleCells(map, currentCell, radiusOut))
            {
                if (!inner.Contains(cell))
                    yield return cell;
            }
        }

        public static IEnumerable<int> GetOutlineCells(MapInstance map, int currentCell, int radius)
        {
            if (map == null) yield break;
            radius = Clamp(radius, 0, MaxRadius);

            if (radius == 0) { yield return currentCell; yield break; }

            var outer = new HashSet<int>(GetCircleCells(map, currentCell, radius));
            var inner = new HashSet<int>(GetCircleCells(map, currentCell, radius - 1));
            foreach (var cell in outer)
            {
                if (!inner.Contains(cell))
                    yield return cell;
            }
        }

        public static IEnumerable<int> GetDiamondRingsCells(MapInstance map, int currentCell, int maxRadius)
        {
            if (map == null) yield break;
            maxRadius = Clamp(maxRadius, 0, MaxRadius);

            var firstLoc9 = maxRadius % 2 == 0 ? 3 : 2;

            for (int loc9 = firstLoc9; loc9 < maxRadius; loc9 += 2)
            {
                var outer    = new HashSet<int>(GetCircleCells(map, currentCell, loc9 + 1));
                var innerSet = new HashSet<int>(GetCircleCells(map, currentCell, loc9));
                foreach (var cell in outer)
                {
                    if (!innerSet.Contains(cell))
                        yield return cell;
                }
            }
        }

        public static IEnumerable<int> GetRectangleCells(MapInstance map, int cellId, int width, int height)
        {
            if (map == null) yield break;
            width  = Clamp(width,  1, MaxRadius);
            height = Clamp(height, 1, MaxRadius);

            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    var cell = cellId + w + h * (map.Width * 2 - 1);
                    if (IsInBounds(map, cell))
                        yield return cell;
                }
            }
        }

        public static IEnumerable<int> GetCrossCells(MapInstance map, int currentCell, int radius)
        {
            if (map == null) yield break;
            radius = Clamp(radius, 0, MaxRadius);

            yield return currentCell;
            foreach (var dir in CardinalDirections)
            {
                for (int i = 1; i <= radius; i++)
                {
                    var next = Pathfinding.NextCell(map, currentCell, dir, i);
                    if (IsInBounds(map, next))
                        yield return next;
                    else
                        break;
                }
            }
        }

        public static IEnumerable<int> GetCrossRingCells(MapInstance map, int currentCell, int radiusIn, int radiusOut)
        {
            if (map == null) yield break;
            radiusIn  = Clamp(radiusIn,  0, MaxRadius);
            radiusOut = Clamp(radiusOut, 0, MaxRadius);
            if (radiusIn > radiusOut) yield break;

            foreach (var dir in CardinalDirections)
            {
                for (int i = radiusIn; i <= radiusOut; i++)
                {
                    var next = Pathfinding.NextCell(map, currentCell, dir, i);
                    if (IsInBounds(map, next))
                        yield return next;
                    else
                        break;
                }
            }
        }

        public static IEnumerable<int> GetTLineCells(MapInstance map, int cellId, DirectionEnum direction, int length)
        {
            if (map == null) yield break;
            length = Clamp(length, 0, MaxRadius);

            var perpDir = (DirectionEnum)(((int)direction + 2) % 8);
            var oppDir  = Pathfinding.OppositeDirection(perpDir);

            yield return cellId;

            for (int i = 1; i <= length; i++)
            {
                var next = Pathfinding.NextCell(map, cellId, perpDir, i);
                if (IsInBounds(map, next))
                    yield return next;
            }

            for (int i = 1; i <= length; i++)
            {
                var next = Pathfinding.NextCell(map, cellId, oppDir, i);
                if (IsInBounds(map, next))
                    yield return next;
            }
        }

        public static IEnumerable<int> GetCells(MapInstance map, int cellId, int currentCell, string range)
        {
            if (map == null || string.IsNullOrEmpty(range))
            {
                yield return cellId;
                yield break;
            }

            switch (range[0])
            {
                case 'C':
                    if (range.Length >= 2 && range[1] == '_')
                    {
                        foreach (var cell in map.Cells)
                            yield return cell.Id;
                        yield break;
                    }
                    else
                    {
                        int r = range.Length >= 2 ? ParseRadius(range[1]) : 0;
                        foreach (var cell in GetCircleCells(map, cellId, r))
                            yield return cell;
                    }
                break;

                case 'X':
                {
                    int r = range.Length >= 2 ? ParseRadius(range[1]) : 0;
                    foreach (var cell in GetCrossCells(map, cellId, r))
                        yield return cell;
                }
                break;

                case 'T':
                {
                    int len = range.Length >= 2 ? ParseRadius(range[1]) : 0;
                    foreach (var cell in GetTLineCells(map, cellId, Pathfinding.GetDirection(map, currentCell, cellId), len))
                        yield return cell;
                    break;
                }

                case 'L':
                {
                    var len = range.Length >= 2 ? ParseRadius(range[1]) : 0;
                    foreach (var cell in GetLineCells(map, cellId, Pathfinding.GetDirection(map, currentCell, cellId), len))
                        yield return cell;
                    break;
                }

                case 'P':
                    yield return cellId;
                break;

                case 'O':
                {
                    int r = range.Length >= 2 ? ParseRadius(range[1]) : 0;
                    foreach (var cell in GetOutlineCells(map, cellId, r))
                        yield return cell;
                    break;
                }

                case 'D':
                {
                    int r = range.Length >= 2 ? ParseRadius(range[1]) : 0;
                    foreach (var cell in GetDiamondRingsCells(map, cellId, r))
                        yield return cell;
                    break;
                }

                case 'R':
                {
                    var w = range.Length >= 2 ? ParseRadius(range[1]) : 1;
                    var h = range.Length >= 3 ? ParseRadius(range[2]) : 1;
                    foreach (var cell in GetRectangleCells(map, cellId, w, h))
                        yield return cell;
                    break;
                }

                default:
                    yield return cellId;
                break;
            }
        }

        private static int ParseRadius(char c)
        {
            var v = Util.HASH.IndexOf(c);
            return Clamp(v < 0 ? 0 : v, 0, MaxRadius);
        }

        private static bool IsInBounds(MapInstance map, int cellId)=> cellId >= 0 && cellId < map.Cells.Count;
        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}
