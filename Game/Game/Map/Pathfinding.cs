
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Protocolo.Framework.Utils;
using Game.Fight;
using Game.Spell;
using Game.Interactive.Type;
using Game.Entity;
using Game.Job;
using Protocolo.Framework.Generic.Logging;

namespace Game.Map
{
    public interface IPriorityQueue<T>
    {
        int Push(T item);
        T Pop();
        T Peek();
        void Update(int i);
    }

    public class PriorityQueueB<T> : IPriorityQueue<T>
    {
        #region "Variables Declaration"
        protected List<T> InnerList = new List<T>();
        protected IComparer<T> mComparer;
        #endregion

        #region "Contructors"
        public PriorityQueueB()
        {
            mComparer = Comparer<T>.Default;
        }

        public PriorityQueueB(IComparer<T> comparer)
        {
            mComparer = comparer;
        }

        public PriorityQueueB(IComparer<T> comparer, int capacity)
        {
            mComparer = comparer;
            InnerList.Capacity = capacity;
        }
        #endregion

        #region "Methods"
        protected void SwitchElements(int i, int j)
        {
            T h = InnerList[i];
            InnerList[i] = InnerList[j];
            InnerList[j] = h;
        }

        protected virtual int OnCompare(int i, int j)
        {
            return mComparer.Compare(InnerList[i], InnerList[j]);
        }

        public int Push(T item)
        {
            int p = InnerList.Count, p2;
            InnerList.Add(item);

            while (p > 0 && OnCompare(p, p2 = (p - 1) / 2) < 0)
            {
                SwitchElements(p, p2);
                p = p2;
            }

            return p;
        }

        public T Pop()
        {
            T result = InnerList[0];
            InnerList[0] = InnerList[InnerList.Count - 1];
            InnerList.RemoveAt(InnerList.Count - 1);

            int p = 0, pn;

            do
            {
                pn = p;
                int p1 = 2 * p + 1;
                int p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0) p = p1;
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0) p = p2;
                if (p != pn) SwitchElements(p, pn);
            }
            while (p != pn);

            return result;
        }

        public void Update(int i)
        {
            int p = i, p2;

            while (p > 0 && OnCompare(p, p2 = (p - 1) / 2) < 0)
            {
                SwitchElements(p, p2);
                p = p2;
            }

            if (p < i) return;

            int pn;
            do
            {
                pn = p;
                int p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0) p = p1;
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0) p = p2;
                if (p != pn) SwitchElements(p, pn);
            }
            while (p != pn);
        }

        public T Peek()
        {
            if (InnerList.Count > 0)
            {
                return InnerList[0];
            }
            return default(T);
        }

        public void Clear()
        {
            InnerList.Clear();
        }

        public int Count => InnerList.Count;

        public void RemoveLocation(T item)
        {
            int index = -1;

            for (int i = 0; i <= InnerList.Count - 1; i++)
            {
                if (mComparer.Compare(InnerList[i], item) == 0)
                {
                    index = i;
                }
            }

            if (index != -1)
            {
                InnerList.RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get { return InnerList[index]; }
            set
            {
                InnerList[index] = value;
                Update(index);
            }
        }
        #endregion
    }

    public class MovementPath
    {
        public List<int> TransitCells
        {
            get;
            private set;
        }

        public List<int> Directions
        {
            get;
            private set;
        }

        public List<int> SegmentLengths
        {
            get;
            private set;
        }

        public int BeginCell => TransitCells.FirstOrDefault();

        public int MovementLength
        {
            get;
            set;
        }

        public double MovementTime
        {
            get
            {
                if (MovementLength <= 0 || SegmentLengths.Count == 0)
                    return 0;
                var speeds = MovementLength > 6 ? Pathfinding.RUN_SPEEDS : Pathfinding.WALK_SPEEDS;
                double total = 0;
                for (int i = 0; i < SegmentLengths.Count; i++)
                {
                    int dir = i * 2 < Directions.Count ? Directions[i * 2] : (Directions.Count > 0 ? Directions[0] : 1);
                    total += (Pathfinding.CELL_PIXEL_DIST[dir] / speeds[dir]) * SegmentLengths[i];
                }
                return total;
            }
        }

        public int LastStep => TransitCells.Count == 0 ? -1 : TransitCells[TransitCells.Count < 2 ? 0 : TransitCells.Count - 2];

        public int EndCell => TransitCells.LastOrDefault();

        private string m_serializedPath;

        public MovementPath()
        {
            TransitCells = new List<int>();
            Directions = new List<int>();
            SegmentLengths = new List<int>();
        }

        public void AddCell(int Cell, int Direction)
        {
            TransitCells.Add(Cell);
            Directions.Add(Direction);
        }

        public void AddSegmentLength(int segmentLength)
        {
            SegmentLengths.Add(segmentLength);
        }

        public int GetDirection(int Cell)
        {
            if (Directions.Count == 0)
                return 1;

            if (TransitCells.Count == 1)
                return Directions[0];

            var index = TransitCells.LastIndexOf(Cell);
            if (index < 0)
                return Directions[Directions.Count - 1];

            index++;
            if (index >= Directions.Count)
                index = Directions.Count - 1;

            return Directions[index];
        }

        public override string ToString()
        {
            if (m_serializedPath == null)
            {
                m_serializedPath = string.Create(TransitCells.Count * 3, this, static (destination, path) =>
                {
                    for (int i = 0; i < path.TransitCells.Count; i++)
                    {
                        var offset = i * 3;
                        destination[offset] = Pathfinding.GetDirectionChar((DirectionEnum)path.Directions[i]);
                        Util.CellToChar(path.TransitCells[i], destination.Slice(offset + 1, 2));
                    }
                });
            }
            return m_serializedPath;
        }
    }

    public enum DirectionEnum : byte
    {
        Noreste = 0,
        Este = 1,
        Sureste = 2,
        Sur = 3,
        Suroeste = 4,
        Oeste = 5,
        Noroeste = 6,
        Norte = 7
    }

    public struct Point
    {
        public double X;
        public double Y;
        public double Z;

        public Point(double x, double y, double z = 0)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    public static class Pathfinding
    {
        private static ILogger Logger = LogManager.GetLogger(typeof(Pathfinding));

        public static double[] RUN_SPEEDS = { 1.700000E-001, 1.500000E-001, 1.500000E-001, 1.500000E-001, 1.700000E-001, 1.500000E-001, 1.500000E-001, 1.500000E-001 };
        public static double[] WALK_SPEEDS = { 7.000000E-002, 6.000000E-002, 6.000000E-002, 6.000000E-002, 7.000000E-002, 6.000000E-002, 6.000000E-002, 6.000000E-002 };
        public static double[] MOUNT_SPEEDS = { 2.300000E-001, 2.000000E-001, 2.000000E-001, 2.000000E-001, 2.300000E-001, 2.000000E-001, 2.000000E-001, 2.000000E-001 };
        internal static readonly double[] CELL_PIXEL_DIST = { 53.0, 29.740, 27.0, 29.740, 53.0, 29.740, 27.0, 29.740 };

        private static DirectionEnum[] FIGHT_DIRECTIONS = { DirectionEnum.Este, DirectionEnum.Sur, DirectionEnum.Oeste, DirectionEnum.Norte };

        public static bool IsValidCellId(MapInstance map, int cell)
        {
            return map != null && cell >= 0 && cell < map.Cells.Count;
        }

        private static ConcurrentDictionary<int, int[]> MapDirections = new ConcurrentDictionary<int, int[]>();
        private static ConcurrentDictionary<int, Point[]> CellPoints = new ConcurrentDictionary<int, Point[]>();

        public static double GetPathTime(int length, int direction)
        {
            var speeds = length > 6 ? RUN_SPEEDS : WALK_SPEEDS;
            return (CELL_PIXEL_DIST[direction] / speeds[direction]) * length;
        }

        public static int GetPathLength(MapInstance map, int beginCell, string encodedPath)
        {
            var lastCell = beginCell;
            var length = 0;

            for (int i = 0; i < encodedPath.Length; i += 3)
            {
                var actualCell = Util.CharToCell(encodedPath.AsSpan(i + 1, 2));
                length += GoalDistance(map, lastCell, actualCell);
                lastCell = actualCell;
            }

            return length;
        }

        public static void GenerateGrid(int width, int cellsCount)
        {
            var grid = new Point[cellsCount];
            for (int i = 0; i < cellsCount; i++)
                grid[i] = new Point(_GetX(width, i), _GetY(width, i));
            CellPoints.TryAdd(cellsCount, grid);
        }

        private static double _GetX(int width, int cell)
        {
            double loc5 = Math.Floor((double)(cell / (width * 2 - 1)));
            double loc6 = cell - loc5 * (width * 2 - 1);
            double loc7 = loc6 % width;

            return (cell - (width - 1) * (loc5 - loc7)) / width;
        }

        private static double _GetY(int width, int cell)
        {
            double loc5 = Math.Floor((double)(cell / (width * 2 - 1)));
            double loc6 = cell - loc5 * (width * 2 - 1);
            double loc7 = loc6 % width;

            return loc5 - loc7;
        }

        public static Point GetPoint(MapInstance map, int cell)
        {
            if (!IsValidCellId(map, cell))
                return new Point(-1000, -1000);

            if (!CellPoints.TryGetValue(map.Cells.Count, out var grid))
            {
                GenerateGrid(map.Width, map.Cells.Count);
                CellPoints.TryGetValue(map.Cells.Count, out grid);
            }

            return grid != null ? grid[cell] : new Point(-1000, -1000);
        }

        public static double GetX(MapInstance map, int cell) => GetPoint(map, cell).X;

        public static double GetY(MapInstance map, int cell) => GetPoint(map, cell).Y;

        public static bool InLine(MapInstance map, int beginCell, int endCell)
        {
            if (!IsValidCellId(map, beginCell) || !IsValidCellId(map, endCell))
                return false;

            var beginPoint = GetPoint(map, beginCell);
            var endPoint = GetPoint(map, endCell);

            return beginPoint.X == endPoint.X || beginPoint.Y == endPoint.Y;
        }

        public static int GoalDistance(MapInstance map, int beginCell, int endCell)
        {
            if (!IsValidCellId(map, beginCell) || !IsValidCellId(map, endCell))
                return int.MaxValue;

            var b = GetPoint(map, beginCell);
            var e = GetPoint(map, endCell);
            return (int)(Math.Abs(e.X - b.X) + Math.Abs(e.Y - b.Y));
        }

        public static char GetDirectionChar(DirectionEnum direction)
        {
            return Util.HASH[(int)direction];
        }

        public static int GetDirection(char direction)
        {
            return Util.HashIndexOf(direction);
        }

        public static int[] GetDirectionChanges(MapInstance map)
        {
            if (map == null)
                return new int[0];

            if (MapDirections.TryGetValue(map.Width, out var cached))
                return cached;

            var directions = new int[] { 1, map.Width, map.Width * 2 - 1, map.Width - 1, -1, -map.Width, -map.Width * 2 + 1, -(map.Width - 1) };

            MapDirections.TryAdd(map.Width, directions);
            return directions;
        }
        /// <summary>
        /// Dirección CARDINAL (Este/Sur/Oeste/Norte) entre dos celdas cualesquiera, por cuadrantes.
        /// Réplica de getDirectionFromCoordinates(..., bAllDirections=false) del cliente 1.29:
        /// es la forma canónica de resolver empujes/atracciones cuando las celdas no están
        /// alineadas (el movimiento de combate solo admite las 4 cardinales).
        /// </summary>
        public static DirectionEnum GetCardinalDirection(MapInstance map, int beginCell, int endCell)
        {
            var beginPoint = GetPoint(map, beginCell);
            var endPoint = GetPoint(map, endCell);
            var dx = endPoint.X - beginPoint.X;
            var dy = endPoint.Y - beginPoint.Y;

            // Ejes exactos primero: atan2(0, -x) devuelve +PI y caería en el cuadrante Sur.
            if (dy == 0)
                return dx >= 0 ? DirectionEnum.Este : DirectionEnum.Oeste;
            if (dx == 0)
                return dy > 0 ? DirectionEnum.Sur : DirectionEnum.Norte;

            var angle = Math.Atan2(dy, dx);

            if (angle > 0 && angle < Math.PI / 2)
                return DirectionEnum.Este;
            if (angle >= Math.PI / 2 && angle < Math.PI)
                return DirectionEnum.Sur;
            if (angle > -Math.PI && angle < -Math.PI / 2)
                return DirectionEnum.Oeste;

            return DirectionEnum.Norte;
        }

        public static DirectionEnum GetDirection(MapInstance map, int beginCell, int dndCell)
        {
            var beginPoint = GetPoint(map, beginCell);
            var endPoint = GetPoint(map, dndCell);
            var dx = endPoint.X - beginPoint.X;
            var dy = endPoint.Y - beginPoint.Y;

            if (dx > 0)
            {
                if (dy < 0) return DirectionEnum.Noreste;
                if (dy == 0) return DirectionEnum.Este;
                return DirectionEnum.Sureste;
            }
            if (dx == 0)
            {
                if (dy > 0) return DirectionEnum.Sur;
                return DirectionEnum.Norte;
            }
            if (dy > 0) return DirectionEnum.Suroeste;
            if (dy == 0) return DirectionEnum.Oeste;
            return DirectionEnum.Noroeste;
        }

        public static MovementPath DecodePath(MapInstance map, int currentCell, string path)
        {
            MovementPath movementPath = new MovementPath();

            if (string.IsNullOrEmpty(path) || path.Length < 3 || path.Length % 3 != 0)
                return movementPath;

            var firstCell = Util.CharToCell(path.AsSpan(1, 2));
            if (GetDirection(path[0]) == -1 || map.GetCell(firstCell) == null)
                return movementPath;

            movementPath.AddCell(currentCell, (int)GetDirection(map, currentCell, firstCell));

            for (int i = 0; i < path.Length; i += 3)
            {
                int curCell = Util.CharToCell(path.AsSpan(i + 1, 2));
                int curDir = Util.HashIndexOf(path[i]);

                if (curDir == -1 || map.GetCell(curCell) == null)
                {
                    movementPath.TransitCells.Clear();
                    movementPath.Directions.Clear();
                    return movementPath;
                }

                movementPath.AddCell(curCell, curDir);
            }

            return movementPath;
        }

        public static DirectionEnum OppositeDirection(DirectionEnum direction)
        {
            return (DirectionEnum)((int)direction >= 4 ? (int)direction - 4 : (int)direction + 4);
        }

        public static int NextCell(MapInstance map, int cellId, DirectionEnum direction, int length = 1)
        {
            switch (direction)
            {
                case DirectionEnum.Noreste: return cellId + (1 * length);
                case DirectionEnum.Este: return cellId + (map.Width * length);
                case DirectionEnum.Sureste: return cellId + (((map.Width * 2) - 1) * length);
                case DirectionEnum.Sur: return cellId + ((map.Width - 1) * length);
                case DirectionEnum.Suroeste: return cellId - (1 * length);
                case DirectionEnum.Oeste: return cellId - (map.Width * length);
                case DirectionEnum.Noroeste: return cellId - (((map.Width * 2) - 1) * length);
                case DirectionEnum.Norte: return cellId - ((map.Width - 1) * length);
                default: return -1;
            }
        }

        // Delta de coordenadas (x,y) de un paso unitario en cada dirección (índice = DirectionEnum).
        private static readonly (int X, int Y)[] DirectionCoordDeltas =
        {
            (1, -1),  // Noreste
            (1, 0),   // Este
            (1, 1),   // Sureste
            (0, 1),   // Sur
            (-1, 1),  // Suroeste
            (-1, 0),  // Oeste
            (-1, -1), // Noroeste
            (0, -1),  // Norte
        };

        /// <summary>
        /// Devuelve la celda a <paramref name="length"/> pasos en una dirección y valida que sea
        /// un vecino REAL comparando coordenadas: <see cref="NextCell"/> es aritmética pura y una
        /// celda junto al borde produce un id en rango pero de la fila contigua (wrap-around, que
        /// hacía que las zonas/empujes saltaran al otro extremo del mapa). Devuelve false si el
        /// resultado se sale del mapa o no cae en la dirección esperada.
        /// </summary>
        public static bool TryGetCellInDirection(MapInstance map, int origin, DirectionEnum direction, int length, out int result)
        {
            result = NextCell(map, origin, direction, length);

            if (map == null || !IsValidCellId(map, origin) || !IsValidCellId(map, result))
                return false;

            var dirIndex = (int)direction;
            if ((uint)dirIndex >= (uint)DirectionCoordDeltas.Length)
                return false;

            var o = GetPoint(map, origin);
            var r = GetPoint(map, result);
            var delta = DirectionCoordDeltas[dirIndex];

            return (int)(r.X - o.X) == delta.X * length && (int)(r.Y - o.Y) == delta.Y * length;
        }

        /// <summary>Comprueba que <paramref name="candidate"/> es un vecino real de
        /// <paramref name="origin"/> en la dirección/índice dado (sin wrap-around).</summary>
        public static bool IsRealNeighbor(MapInstance map, int origin, int candidate, int directionIndex)
        {
            if (map == null || !IsValidCellId(map, origin) || !IsValidCellId(map, candidate))
                return false;

            if ((uint)directionIndex >= (uint)DirectionCoordDeltas.Length)
                return false;

            var o = GetPoint(map, origin);
            var c = GetPoint(map, candidate);
            var delta = DirectionCoordDeltas[directionIndex];

            return (int)(c.X - o.X) == delta.X && (int)(c.Y - o.Y) == delta.Y;
        }

        public static MovementPath IsValidPath(AbstractEntity entity, MapInstance map, int currentCell, string encodedPath)
        {
            if (entity == null || map == null || string.IsNullOrEmpty(encodedPath))
                return null;

            MovementPath decodedPath = DecodePath(map, currentCell, encodedPath);

            if (decodedPath.TransitCells.Count < 2)
                return null;

            var finalPath = new MovementPath();
            var index = 0;
            int transitCell = 0;
            int nextTransitCell = 0;
            DirectionEnum direction = DirectionEnum.Noreste;

            do
            {
                transitCell = decodedPath.TransitCells[index];
                nextTransitCell = decodedPath.TransitCells[index + 1];
                direction = (DirectionEnum)decodedPath.GetDirection(transitCell);
                var length = Pathfinding.IsValidLine(entity, map, finalPath, transitCell, direction, nextTransitCell, decodedPath.EndCell);
                if (length == -1)
                    return null;
                else if (length == -2)
                    break;
                index++;
            }
            while (transitCell != decodedPath.LastStep);

            return finalPath;
        }

        public static MovementPath IsValidPath(AbstractFight fight, AbstractFighter fighter, int currentCell, string encodedPath)
        {
            if (fight?.Map == null || fighter == null || string.IsNullOrEmpty(encodedPath))
                return null;

            var decodedPath = DecodePath(fight.Map, currentCell, encodedPath);
            if (decodedPath.TransitCells.Count < 2)
                return null;
            var finalPath = new MovementPath();

            var index = 0;
            int transitCell = 0;
            do
            {
                transitCell = decodedPath.TransitCells[index];
                var length = Pathfinding.IsValidLine(fight, fighter, finalPath, transitCell, (DirectionEnum)decodedPath.GetDirection(transitCell), decodedPath.TransitCells[index + 1]);
                if (length == -1)
                    return null;
                else if (length == -2)
                    break;
                index++;
            }
            while (transitCell != decodedPath.LastStep);

            return finalPath;
        }

        public static int IsValidLine(AbstractEntity entity, MapInstance map, MovementPath finalPath, int beginCell, DirectionEnum direction, int endCell, int finalCell)
        {
            if (map.GetCell(beginCell) == null || map.GetCell(endCell) == null)
                return -1;

            var isCharacter = entity.Type == EntityTypeEnum.TYPE_CHARACTER;
            var character = isCharacter ? (CharacterEntity)entity : null;

            var actualCell = beginCell;
            var lastCell = beginCell;
            var length = -1;

            finalPath.AddCell(beginCell, (int)direction);

            bool blocked;
            do
            {
                actualCell = Pathfinding.NextCell(map, actualCell, direction);

                var mapCell = map.GetCell(actualCell);
                var io = mapCell?.InteractiveObject;

                blocked = mapCell == null || (io != null && (!io.CanWalkThrough || (isCharacter && actualCell == finalCell && io.IsActive))) || (!mapCell.Walkable && !map.IsAnimatedDoorOpen(actualCell)) || (isCharacter && map.HasAggroNear(character, lastCell)) || (mapCell.IsDestinationOnly && actualCell != finalCell);

                if (!blocked)
                {
                    length++;
                    lastCell = actualCell;
                    finalPath.MovementLength++;
                }
            }
            while (!blocked && actualCell != endCell);

            if (blocked)
                length = -2;

            finalPath.AddCell(lastCell, (int)direction);

            if (length >= 0)
                finalPath.AddSegmentLength(length + 1);

            return length;
        }

        public static int IsValidLine(AbstractFight fight, AbstractFighter fighter, MovementPath path, int beginCell, DirectionEnum direction, int endCell)
        {
            if ((direction != DirectionEnum.Este && direction != DirectionEnum.Sur && direction != DirectionEnum.Oeste && direction != DirectionEnum.Norte) || fight.GetCell(beginCell) == null || fight.GetCell(endCell) == null)
                return -1;

            var length = -1;
            var actualCell = beginCell;

            if (!Pathfinding.InLine(fight.Map, beginCell, endCell))
                return length;

            length = (int)GoalDistance(fight.Map, beginCell, endCell);

            path.AddCell(actualCell, (int)direction);

            var prevCell = actualCell;
            for (int i = 0; i < length; i++)
            {
                actualCell = Pathfinding.NextCell(fight.Map, actualCell, direction);

                if (!fight.Map.IsWalkable(actualCell))
                    return -2;

                var prevFightCell = fight.GetCell(prevCell);
                var curFightCell = fight.GetCell(actualCell);
                if (prevFightCell != null && curFightCell != null &&
                    Math.Abs(curFightCell.GroundLevel - prevFightCell.GroundLevel) > 2)
                    return -2;

                var mapCell = fight.Map.GetCell(actualCell);
                if (mapCell != null && mapCell.IsDestinationOnly && actualCell != endCell)
                    return -2;

                // NINGUN luchador es atravesable, tampoco un invisible: el que anda choca con el y
                // se detiene justo antes (return -2 conserva el camino recorrido hasta aqui). Ese
                // choque es precisamente la forma de localizar a un Sram invisible.
                if (fight.GetFighterOnCell(actualCell) != null)
                    return -2;

                path.AddCell(actualCell, (int)direction);
                path.MovementLength++;
                prevCell = actualCell;

                if (Pathfinding.IsStopCell(fighter.Fight, fighter.Team, actualCell))
                    return -2;
            }

            return length;
        }

        public static int TryTacle(AbstractFighter fighter)
        {
            // Un luchador invisible no puede ser placado: nadie lo ve para bloquearle el paso.
            if (fighter.StateManager != null && fighter.StateManager.HasState(FighterStateEnum.STATE_STEALTH))
                return -1;

            var enemies = GetEnnemiesNear(fighter.Fight, fighter.Team, fighter.Cell.Id);
            if (!enemies.Any() || enemies.All(e => e.StateManager.HasState(FighterStateEnum.STATE_ROOTED)))
                return -1;

            int fighterAgility = fighter.Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD);
            int enemiesAgility = enemies.Where(e => !e.StateManager.HasState(FighterStateEnum.STATE_ROOTED)).Sum(e => e.Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD));
            int A = fighterAgility + 25;
            int B = Math.Max(1, fighterAgility + enemiesAgility + 50);
            int chance = (int)((long)(300 * A / B) - 100);
            int rand = FastRandom.Shared.Next(0, 99);
            return rand > chance ? rand : -1;
        }

        public static bool IsStopCell(AbstractFight fight, FightTeam team, int cellId) =>
            fight.GetCell(cellId).HasObject(FightObstacleTypeEnum.TYPE_TRAP) || GetEnnemiesNear(fight, team, cellId).Any();

        public static IEnumerable<AbstractFighter> GetEnnemiesNear(AbstractFight fight, FightTeam team, int cellId)
        {
            // Un enemigo INVISIBLE no cuenta como amenaza adyacente: no placa, no corta el
            // movimiento (IsStopCell) ni delata su posicion por zona de placaje.
            return GetFightersNear(fight, cellId).Where(fighter => fighter.Team != team
                && (fighter.StateManager == null || !fighter.StateManager.HasState(FighterStateEnum.STATE_STEALTH)));
        }

        public static List<AbstractFighter> GetFightersNear(AbstractFight fight, int cellId)
        {
            var fighters = new List<AbstractFighter>(FIGHT_DIRECTIONS.Length);
            for (int i = 0; i < FIGHT_DIRECTIONS.Length; i++)
            {
                var fighter = fight.GetFighterOnCell(NextCell(fight.Map, cellId, FIGHT_DIRECTIONS[i]));
                if (fighter != null && !fighter.IsFighterDead)
                    fighters.Add(fighter);
            }

            return fighters;
        }
        
        // Bonus de altura de un "sprite" (luchador) sobre una celda, como en el cliente: el ojo
        // del que mira y del objetivo está 1.5 por encima del suelo, lo que permite ver por
        // encima de obstáculos bajos entre dos luchadores.
        private const double SpriteEyeHeight = 1.5;

        /// <summary>
        /// Línea de visión idéntica a la del cliente 1.29 (ank.battlefield.utils.Pathfinding.
        /// checkView): barrido por columnas con interpolación de altura del terreno y de los
        /// luchadores intermedios. El Bresenham entero anterior visitaba celdas distintas y
        /// bloqueaba en roces de esquina donde el cliente sí ve, produciendo RESULT_NO_LOS falsos.
        /// Se usa <see cref="double"/> igual que el AS (Number) para que el resultado coincida.
        /// </summary>
        public static bool CheckView(AbstractFight fight, int beginCell, int endCell)
        {
            return CheckView(fight, beginCell, endCell, null, -1);
        }

        /// <summary>
        /// Misma vista, pero evaluada sobre el tablero que HABRÁ cuando <paramref name="movedFighter"/>
        /// esté en <paramref name="movedToCell"/>: ocupa la celda de destino (su ojo cuenta como
        /// sprite) y deja libre la actual (su propio cuerpo ya no tapa la línea). Lo necesita la IA,
        /// que decide si podrá lanzar DESPUÉS de moverse; el jugador lanza desde donde ya está y usa
        /// la sobrecarga sin proyección, que es exactamente el tablero real.
        /// </summary>
        public static bool CheckView(AbstractFight fight, int beginCell, int endCell, AbstractFighter movedFighter, int movedToCell)
        {
            if (fight?.Map == null || beginCell == endCell)
                return true;

            if (!IsValidCellId(fight.Map, beginCell) || !IsValidCellId(fight.Map, endCell))
                return false;

            var p1 = GetPoint(fight.Map, beginCell);
            var p2 = GetPoint(fight.Map, endCell);

            var z1 = CellHeight(fight, beginCell) + (HasSprite(fight, beginCell, movedFighter, movedToCell) ? SpriteEyeHeight : 0.0);
            var z2 = CellHeight(fight, endCell) + (HasSprite(fight, endCell, movedFighter, movedToCell) ? SpriteEyeHeight : 0.0);
            var zDiff = z2 - z1;

            var d = Math.Max(Math.Abs(p1.Y - p2.Y), Math.Abs(p1.X - p2.X));
            var m = (p1.Y - p2.Y) / (p1.X - p2.X);
            var b = p1.Y - m * p1.X;
            var xStep = (p2.X - p1.X < 0) ? -1 : 1;
            var yStep = (p2.Y - p1.Y < 0) ? -1 : 1;

            var curY = p1.Y;
            var endX = p2.X * xStep;
            var curX = p1.X + 0.5 * xStep;

            while (true)
            {
                curX += xStep;
                if (curX * xStep > endX)
                    break;

                var yAtX = m * curX + b;
                double yNext, yEdge;
                if (yStep > 0)
                {
                    yNext = AsRound(yAtX);
                    yEdge = Math.Ceiling(yAtX - 0.5);
                }
                else
                {
                    yNext = Math.Ceiling(yAtX - 0.5);
                    yEdge = AsRound(yAtX);
                }

                var innerY = curY;
                while (true)
                {
                    innerY += yStep;
                    if (innerY * yStep > yEdge * yStep)
                        break;
                    if (!CheckCellView(fight, curX - xStep / 2.0, innerY, false, p1, z1, p2, zDiff, d, movedFighter, movedToCell))
                        return false;
                }
                curY = yNext;
            }

            var lastY = curY;
            while (true)
            {
                lastY += yStep;
                if (lastY * yStep > p2.Y * yStep)
                    break;
                if (!CheckCellView(fight, curX - 0.5 * xStep, lastY, false, p1, z1, p2, zDiff, d, movedFighter, movedToCell))
                    return false;
            }

            // El último tramo (celda del objetivo) siempre concede visión.
            return CheckCellView(fight, curX - 0.5 * xStep, lastY - yStep, true, p1, z1, p2, zDiff, d, movedFighter, movedToCell);
        }

        private static bool CheckCellView(AbstractFight fight, double x, double y, bool isLast, Point p1, double p1z, Point p2, double zDiff, double d, AbstractFighter movedFighter, int movedToCell)
        {
            var cellId = GetCell1(fight.Map, x, y);
            var mapCell = fight.Map.GetCell(cellId);

            var dist = Math.Max(Math.Abs(p1.Y - y), Math.Abs(p1.X - x));
            var zThreshold = dist / d * zDiff + p1z;
            var cellHeight = mapCell != null ? mapCell.GroundLevel - 7 : 0.0;
            var lineOfSight = mapCell != null && mapCell.LineOfSight;

            // Un luchador intermedio bloquea, salvo en la celda de origen (dist 0), la del objetivo
            // o la última: como en el cliente, uno no se tapa la vista a sí mismo ni a su objetivo.
            var spriteBlocks = mapCell != null
                && !(dist == 0 || isLast || (p2.X == x && p2.Y == y))
                && HasSprite(fight, cellId, movedFighter, movedToCell);

            if (lineOfSight && cellHeight <= zThreshold && !spriteBlocks)
                return true;

            return isLast;
        }

        // Altura del terreno como getCellHeight del cliente (groundLevel - 7). El servidor no
        // guarda la pendiente (groundSlope), que añadiría hasta +0.5; omitirla solo puede volver
        // la vista MÁS permisiva, nunca genera bloqueos falsos.
        private static double CellHeight(AbstractFight fight, int cellId)
        {
            var mapCell = fight.Map.GetCell(cellId);
            return mapCell != null ? mapCell.GroundLevel - 7 : 0.0;
        }

        private static bool HasSprite(AbstractFight fight, int cellId)
        {
            var fighter = fight.GetFighterOnCell(cellId);
            return fighter != null && !fighter.IsFighterDead;
        }

        // Ocupación de la celda en el tablero proyectado: movedFighter ya está en su destino y ha
        // dejado libre su celda actual. Sin proyección (movedFighter null) es el tablero real, que
        // es lo que ve el jugador.
        private static bool HasSprite(AbstractFight fight, int cellId, AbstractFighter movedFighter, int movedToCell)
        {
            if (movedFighter != null && !movedFighter.IsFighterDead)
            {
                if (cellId == movedToCell)
                    return true;

                // Solo cabe un luchador por celda: si la que deja atrás es esta, queda vacía.
                if (movedFighter.Cell != null && cellId == movedFighter.Cell.Id)
                    return false;
            }

            return HasSprite(fight, cellId);
        }

        // Math.round del cliente (ActionScript): redondeo al entero más próximo con el 0.5 hacia
        // +infinito, distinto del redondeo bancario por defecto de .NET.
        private static double AsRound(double value) => Math.Floor(value + 0.5);

        public static int GetCell1(MapInstance map, double x, double y) => (int)Math.Round(x) * map.Width + (int)Math.Round(y) * (map.Width - 1);

    }

    public struct PathNode
    {
        // Marca de generación: un nodo se considera "sin visitar" si su Gen no coincide con la
        // de la búsqueda actual. Evita limpiar toda la grilla (O(celdas)) en cada llamada.
        public int Gen;
        public int Parent;
        public int Dir;      // dirección (0-7) por la que se llegó; -1 en el origen.
        public double G;     // pasos ponderados (1 recto / 1.5 diagonal): acota los PM.
        public double V;     // coste acumulado con pesos de celda (modelo del cliente): ordena.
        public double F;     // V + heurística.
        public NodeState Status;
    }

    public enum NodeState : byte
    {
        None = 0,
        InOpenList,
        InCloseList
    }

    /// <summary>
    /// A* de combate/mundo. Reproduce el modelo de coste del cliente 1.29
    /// (ank.battlefield.utils.Pathfinding): pondera cada celda por su valor de movimiento y
    /// penaliza los cambios de dirección, de modo que las rutas generadas por el servidor
    /// coincidan con las que predice el cliente. No es thread-safe: se asume el hilo de juego.
    /// </summary>
    public class Pathmaker
    {
        public MapInstance map;
        private readonly int cellCount;
        private readonly int[] directions;

        // Direcciones válidas de combate (cardinales E/S/O/N = índices 1,3,5,7) y las 8 completas.
        // El original recorría {0,1,2,3} (NE/E/SE/S, todas de offset positivo): un luchador solo
        // podía trazar ruta hacia celdas de índice mayor, por eso no se acercaba al objetivo.
        private static readonly int[] CardinalOrder = { 1, 3, 5, 7 };
        private static readonly int[] AllDirectionsOrder = { 1, 3, 5, 7, 0, 2, 4, 6 };

        private PathNode[] m_grid;
        private PriorityQueueB<int> m_openList;
        private int m_generation;

        public Pathmaker(MapInstance mapInstance)
        {
            map = mapInstance;
            cellCount = map.Cells.Count;

            directions = new int[] { 1, map.Width, map.Width * 2 - 1, map.Width - 1, -1, -map.Width, -(map.Width * 2 - 1), -(map.Width - 1) };
        }

        public string FindPathAsString(int startCell, int endCell, bool diagonal, int movementPoints = -1, IEnumerable<int> obstacles = null)
        {
            var pathList = FindPath(startCell, endCell, diagonal, movementPoints, obstacles);
            return EncodePath(pathList);
        }

        private string EncodePath(List<int> pathList)
        {
            var segmentCount = Math.Max(0, pathList.Count - 1);
            return string.Create(segmentCount * 3, (PathList: pathList, Map: map), static (destination, state) =>
            {
                for (int i = 0; i < state.PathList.Count - 1; i++)
                {
                    var offset = i * 3;
                    destination[offset] = Pathfinding.GetDirectionChar(Pathfinding.GetDirection(state.Map, state.PathList[i], state.PathList[i + 1]));
                    Util.CellToChar(state.PathList[i + 1], destination.Slice(offset + 1, 2));
                }
            });
        }

        public List<int> FindPath(int startCell, int endCell, bool diagonal, int movementPoints = -1, IEnumerable<int> obstacles = null)
        {
            return FindPath(startCell, endCell, diagonal, movementPoints, obstacles, out _);
        }

        /// <summary>
        /// Calcula la ruta y expone si alcanzó exactamente el destino (<paramref name="reachedGoal"/>).
        /// Si no lo alcanza, devuelve la mejor aproximación (celda visitada más cercana), como el
        /// cliente. Los que necesiten un camino exacto deben comprobar la bandera.
        /// </summary>
        public List<int> FindPath(int startCell, int endCell, bool diagonal, int movementPoints, IEnumerable<int> obstacles, out bool reachedGoal)
        {
            reachedGoal = false;

            if (!Pathfinding.IsValidCellId(map, startCell) || !Pathfinding.IsValidCellId(map, endCell))
                return new List<int>();

            if (startCell == endCell)
            {
                reachedGoal = true;
                return new List<int> { startCell };
            }

            if (movementPoints == 0)
                return new List<int>();

            var blocked = BuildBlockedSet(obstacles);

            EnsureBuffers();
            NextGeneration();

            var endPoint = Pathfinding.GetPoint(map, endCell);
            var dirOrder = diagonal ? AllDirectionsOrder : CardinalOrder;

            ref var start = ref Node(startCell);
            start.G = 0;
            start.V = 0;
            start.F = Heuristic(startCell, endPoint);
            start.Parent = -1;
            start.Dir = -1;
            start.Status = NodeState.InOpenList;
            m_openList.Push(startCell);

            var success = false;
            var bestCell = startCell;
            var bestH = start.F;
            var location = startCell;

            while (m_openList.Count > 0)
            {
                location = m_openList.Pop();

                ref var current = ref Node(location);
                if (current.Status == NodeState.InCloseList)
                    continue;

                if (location == endCell)
                {
                    current.Status = NodeState.InCloseList;
                    success = true;
                    break;
                }

                current.Status = NodeState.InCloseList;

                var currentH = current.F - current.V;
                if (currentH < bestH)
                {
                    bestH = currentH;
                    bestCell = location;
                }

                var currentCell = map.GetCell(location);
                var currentGroundLevel = currentCell?.GroundLevel ?? 0;
                var currentG = current.G;
                var currentV = current.V;
                var currentDir = current.Dir;

                for (int d = 0; d < dirOrder.Length; d++)
                {
                    int i = dirOrder[d];
                    int neighbor = location + directions[i];

                    // Vecino real (sin wrap-around por el borde): equivale al chequeo de
                    // coordenadas del cliente (|x1-x2| <= 53). Descarta que el A* "cruce" el mapa.
                    if (!Pathfinding.IsRealNeighbor(map, location, neighbor, i))
                        continue;

                    var isEnd = neighbor == endCell;

                    if (!isEnd && (!map.IsWalkable(neighbor) || blocked != null && blocked.Contains(neighbor)))
                        continue;

                    var neighborCell = map.GetCell(neighbor);
                    if (neighborCell == null)
                        continue;

                    // No se puede saltar entre celdas con más de 2 niveles de desnivel.
                    if (Math.Abs(neighborCell.GroundLevel - currentGroundLevel) > 2)
                        continue;

                    // Las celdas "solo destino" no se atraviesan, solo se pueden usar como final.
                    if (neighborCell.IsDestinationOnly && !isEnd)
                        continue;

                    var stepCost = (i & 1) == 0 ? 1.5 : 1.0; // índices pares = diagonales.
                    var newG = currentG + stepCost;

                    // Como el cliente, ninguna celda (tampoco el destino) se alcanza más allá de
                    // los PM: si el objetivo queda fuera, se devuelve la mejor aproximación.
                    if (movementPoints > 0 && newG > movementPoints)
                        continue;

                    var newV = currentV + stepCost + StepWeight(neighborCell, i, currentDir, isEnd);

                    ref var neighborNode = ref Node(neighbor);
                    if ((neighborNode.Status != NodeState.None) && neighborNode.V <= newV)
                        continue;

                    neighborNode.Parent = location;
                    neighborNode.Dir = i;
                    neighborNode.G = newG;
                    neighborNode.V = newV;
                    neighborNode.F = newV + Heuristic(neighbor, endPoint);
                    neighborNode.Status = NodeState.InOpenList;
                    m_openList.Push(neighbor);
                }
            }

            var target = success ? endCell : bestCell;
            reachedGoal = success;

            return BuildPath(startCell, target);
        }

        // Modelo de coste del cliente: prefiere celdas con mayor valor de movimiento y castiga
        // los cambios de dirección (rutas rectas), reproduciendo el trazado que dibuja el cliente.
        private static double StepWeight(MapCell neighborCell, int dir, int cameFromDir, bool isEnd)
        {
            var movement = neighborCell.Movement;

            // Solo-destino/obstáculo usados como celda final: fuertemente preferidos como cierre.
            if (movement <= 1)
                return isEnd ? -1000.0 : 1000.0 + ((dir & 1) == 0 ? 3.0 : 0.0);

            var directionChange = dir != cameFromDir ? 0.5 : 0.0;
            return directionChange + (5 - movement) / 3.0;
        }

        private double Heuristic(int cell, Point endPoint)
        {
            // Igual que el cliente (goalDistEstimate): distancia euclídea en coordenadas de celda.
            // Admisible respecto al coste por pasos, por lo que el A* sigue siendo óptimo.
            var point = Pathfinding.GetPoint(map, cell);
            var dx = point.X - endPoint.X;
            var dy = point.Y - endPoint.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void EnsureBuffers()
        {
            if (m_grid == null)
            {
                m_grid = new PathNode[cellCount + 1];
                m_openList = new PriorityQueueB<int>(new ComparePFNodeMatrix(m_grid));
            }
        }

        private void NextGeneration()
        {
            m_openList.Clear();

            // Al desbordar el contador se reinicia la grilla una única vez.
            if (++m_generation == 0)
            {
                Array.Clear(m_grid, 0, m_grid.Length);
                m_generation = 1;
            }
        }

        private ref PathNode Node(int cell)
        {
            ref var node = ref m_grid[cell];
            if (node.Gen != m_generation)
            {
                node.Gen = m_generation;
                node.Status = NodeState.None;
                node.Parent = -1;
                node.Dir = -1;
                node.G = 0;
                node.V = 0;
                node.F = 0;
            }
            return ref node;
        }

        private HashSet<int> BuildBlockedSet(IEnumerable<int> obstacles)
        {
            if (obstacles == null)
                return null;

            if (obstacles is HashSet<int> set)
                return set.Count == 0 ? null : set;

            HashSet<int> result = null;
            foreach (var cell in obstacles)
            {
                if (!Pathfinding.IsValidCellId(map, cell))
                    continue;
                result ??= new HashSet<int>();
                result.Add(cell);
            }
            return result;
        }

        private List<int> BuildPath(int startCell, int targetCell)
        {
            // Reconstrucción start..target siguiendo la cadena de padres, sin invertir una lista
            // intermedia: se cuenta la longitud y se rellena desde el final.
            var length = 1;
            var cursor = targetCell;
            while (cursor != startCell)
            {
                var parent = Node(cursor).Parent;
                if (parent < 0)
                {
                    // El destino no está conectado con el origen (solo puede pasar con bestCell
                    // == start, ya cubierto): devolver únicamente el origen.
                    return new List<int> { startCell };
                }
                cursor = parent;
                length++;
            }

            var path = new int[length];
            cursor = targetCell;
            for (int i = length - 1; i >= 0; i--)
            {
                path[i] = cursor;
                if (cursor != startCell)
                    cursor = Node(cursor).Parent;
            }

            return new List<int>(path);
        }

        internal class ComparePFNodeMatrix : IComparer<int>
        {
            private readonly PathNode[] mMatrix;

            public ComparePFNodeMatrix(PathNode[] matrix)
            {
                mMatrix = matrix;
            }

            public int Compare(int a, int b)
            {
                if (mMatrix[a].F > mMatrix[b].F)
                {
                    return 1;
                }
                else if (mMatrix[a].F < mMatrix[b].F)
                {
                    return -1;
                }
                return 0;
            }
        }
    }
}


