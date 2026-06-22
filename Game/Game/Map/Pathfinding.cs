
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

        private static FastRandom PATHFIND_RANDOM = new FastRandom();
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
            var enemies = GetEnnemiesNear(fighter.Fight, fighter.Team, fighter.Cell.Id);
            if (!enemies.Any() || enemies.All(e => e.StateManager.HasState(FighterStateEnum.STATE_ROOTED)))
                return -1;

            int fighterAgility = fighter.Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD);
            int enemiesAgility = enemies.Where(e => !e.StateManager.HasState(FighterStateEnum.STATE_ROOTED)).Sum(e => e.Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD));
            int A = fighterAgility + 25;
            int B = Math.Max(1, fighterAgility + enemiesAgility + 50);
            int chance = (int)((long)(300 * A / B) - 100);
            int rand = PATHFIND_RANDOM.Next(0, 99);
            return rand > chance ? rand : -1;
        }

        public static bool IsStopCell(AbstractFight fight, FightTeam team, int cellId) =>
            fight.GetCell(cellId).HasObject(FightObstacleTypeEnum.TYPE_TRAP) || GetEnnemiesNear(fight, team, cellId).Any();

        public static IEnumerable<AbstractFighter> GetEnnemiesNear(AbstractFight fight, FightTeam team, int cellId)
        {
            return GetFightersNear(fight, cellId).Where(fighter => fighter.Team != team);
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
        
        private static bool BresenhamLine(AbstractFight fight, int beginCell, int endCell)
        {
            if (beginCell == endCell)
                return true;

            var begin = GetPoint(fight.Map, beginCell);
            var end = GetPoint(fight.Map, endCell);
            int x0 = (int)begin.X, y0 = (int)begin.Y, x1 = (int)end.X, y1 = (int)end.Y;

            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int x = x0, y = y0;
            int xi = x1 > x0 ? 1 : -1;
            int yi = y1 > y0 ? 1 : -1;
            int err = dx - dy;
            int dx2 = dx * 2, dy2 = dy * 2;
            int steps = dx + dy;

            for (int i = 0; i < steps;)
            {
                if (err > 0)
                {
                    x += xi;
                    err -= dy2;
                }
                else if (err < 0)
                {
                    y += yi;
                    err += dx2;
                }
                else
                {
                    if (!IsTransparent(fight, GetCell1(fight.Map, x + xi, y), beginCell, endCell))
                        return false;
                    if (!IsTransparent(fight, GetCell1(fight.Map, x, y + yi), beginCell, endCell))
                        return false;

                    x += xi;
                    y += yi;
                    err -= dy2;
                    err += dx2;
                    i++;
                }

                if (!IsTransparent(fight, GetCell1(fight.Map, x, y), beginCell, endCell))
                    return false;

                i++;
            }

            return true;
        }
        
        private static bool IsTransparent(AbstractFight fight, int cellId, int beginCell, int endCell)
        {
            if (cellId == beginCell || cellId == endCell)
                return true;

            var fightCell = fight.GetCell(cellId);
            if (fightCell == null)
                return true;

            return fightCell.LineOfSight && !fightCell.HasObject(FightObstacleTypeEnum.TYPE_FIGHTER);
        }

        public static bool CheckView(AbstractFight fight, int beginCell, int endCell) => BresenhamLine(fight, beginCell, endCell);

        public static int GetCell1(MapInstance map, double x, double y) => (int)x * map.Width + (int)y * (map.Width - 1);

    }

    public struct PathNode
    {
        public int Cell;
        public double F;
        public double G;
        public int Parent;
        public NodeState Status;
    }

    public enum NodeState : byte
    {
        None = 0,
        InOpenList,
        InCloseList
    }

    public class Pathmaker
    {
        public const int estimatedHeuristic = 1;
        public MapInstance map;
        private int cellCount;

        private int[] directions;
        private PathNode[] CalcGrid;
        private PriorityQueueB<int> OpenList;
        private List<PathNode> ClosedList;

        public Pathmaker(MapInstance mapInstance)
        {
            map = mapInstance;
            cellCount = map.Cells.Count;

            directions = new int[] { 1, map.Width, map.Width * 2 - 1, map.Width - 1, -1, -map.Width, -(map.Width * 2 - 1), -(map.Width - 1) };


        }

        public string FindPathAsString(int startCell, int endCell, bool diagonal, int movementPoints = -1, IEnumerable<int> obstacles = null)
        {
            var pathList = FindPath(startCell, endCell, diagonal, movementPoints, obstacles == null ? new List<int>() : obstacles);

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

        private List<int> FindPath(int StartCell, int EndCell, bool Diagonal, int MovementPoints = -1, IEnumerable<int> Obstacles = null)
        {
            var blockedCells = Obstacles == null ? new HashSet<int>() : Obstacles.Where(cell => Pathfinding.IsValidCellId(map, cell)).ToHashSet();

            if (!Pathfinding.IsValidCellId(map, StartCell) || !Pathfinding.IsValidCellId(map, EndCell))
                return new List<int>();

            if (StartCell == EndCell)
                return new List<int> { StartCell };

            if (MovementPoints == 0)
                return new List<int>();

            if (CalcGrid == null)
            {
                CalcGrid = new PathNode[cellCount + 1];
                OpenList = new PriorityQueueB<int>(new ComparePFNodeMatrix(CalcGrid));
                ClosedList = new List<PathNode>();
            }
            else
            {
                Array.Clear(CalcGrid, 0, CalcGrid.Length);
                OpenList.Clear();
                ClosedList.Clear();
            }

            Point StartPoint = Pathfinding.GetPoint(map, StartCell);
            Point EndPoint = Pathfinding.GetPoint(map, EndCell);

            bool Success = false;
            int BestLocation = StartCell;
            int Location = StartCell;

            CalcGrid[Location].Cell = Location;
            CalcGrid[Location].G = 0;
            CalcGrid[Location].F = estimatedHeuristic;
            CalcGrid[Location].Parent = -1;
            CalcGrid[Location].Status = NodeState.InOpenList;

            OpenList.Push(Location);
            while (OpenList.Count > 0 && !Success)
            {
                Location = OpenList.Pop();
                if (!Pathfinding.IsValidCellId(map, Location))
                    continue;

                if (CalcGrid[Location].Status == NodeState.InCloseList)
                    continue;

                if (Location == EndCell)
                {
                    CalcGrid[Location].Status = NodeState.InCloseList;
                    Success = true;
                    break;
                }

                Point LocationPoint = Pathfinding.GetPoint(map, Location);

                int maxDir = Diagonal ? 8 : 4;
                for (int i = 0; i < maxDir; i++)
                {
                    int NewLocation = Location + directions[i];
                    if (!Pathfinding.IsValidCellId(map, NewLocation))
                        continue;

                    if ((!map.IsWalkable(NewLocation) || blockedCells.Contains(NewLocation)) && NewLocation != EndCell)
                        continue;

                    var newMapCell = map.GetCell(NewLocation);
                    var curMapCell = map.GetCell(Location);


                    if (newMapCell != null && curMapCell != null &&
                        Math.Abs(newMapCell.GroundLevel - curMapCell.GroundLevel) > 2)
                        continue;


                    if (newMapCell != null && newMapCell.IsDestinationOnly && NewLocation != EndCell)
                        continue;


                    double stepCost = i < 4 ? 1.0 : 1.5;
                    double NewG = CalcGrid[Location].G + stepCost;

                    if ((CalcGrid[NewLocation].Status == NodeState.InOpenList || CalcGrid[NewLocation].Status == NodeState.InCloseList)
                        && CalcGrid[NewLocation].G <= NewG)
                        continue;

                    Point NewLocationPoint = Pathfinding.GetPoint(map, NewLocation);

                    CalcGrid[NewLocation].Cell = NewLocation;
                    CalcGrid[NewLocation].Parent = Location;
                    CalcGrid[NewLocation].G = NewG;

                    double H = Math.Abs(NewLocationPoint.X - EndPoint.X) + Math.Abs(NewLocationPoint.Y - EndPoint.Y);

                    double Cross = Math.Abs((NewLocationPoint.X - EndPoint.X) * (StartPoint.Y - EndPoint.Y) - (StartPoint.X - EndPoint.X) * (NewLocationPoint.Y - EndPoint.Y));

                    CalcGrid[NewLocation].F = NewG + H + Cross;
                    CalcGrid[NewLocation].Status = NodeState.InOpenList;
                    OpenList.Push(NewLocation);
                }

                if (BestLocation == -1 || Pathfinding.GoalDistance(map, Location, EndCell) < Pathfinding.GoalDistance(map, BestLocation, EndCell))
                    BestLocation = Location;

                CalcGrid[Location].Status = NodeState.InCloseList;
            }

            if (!Success)
                EndCell = BestLocation;

            var Node = CalcGrid[EndCell];
            while (Node.Parent != -1)
            {
                ClosedList.Add(Node);
                Node = CalcGrid[Node.Parent];
            }
            ClosedList.Add(Node);
            ClosedList.Reverse();

            int take = (MovementPoints > 0 && ClosedList.Count - 1 >= MovementPoints) ? MovementPoints + 1 : ClosedList.Count;

            var result = new List<int>(take);
            for (int i = 0; i < take; i++)
                result.Add(ClosedList[i].Cell);
            return result;
        }

        internal class ComparePFNodeMatrix : IComparer<int>
        {
            private PathNode[] mMatrix;

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


