
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocolo.Framework.Utils;
using Game.Network;
using Game.Fight;
using Game.Spell;
using Game.Interactive.Type;
using Game.Entity;
using Game.Job;
using Protocolo.Framework.Generic.Logging;

namespace Game.Map
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPriorityQueue<T>
    {
        int Push(T item);
        T Pop();
        T Peek();
        void Update(int i);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

        /// <summary>
        /// Push an object onto the PQ
        /// </summary>
        /// <param name="O">The new object</param>
        /// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
        public int Push(T item)
        {
            int p = InnerList.Count;
            int p2 = 0;
            InnerList.Add(item);
            // E[p] = O
            do
            {
                if (p == 0)
                {
                    break; // TODO: might not be correct. Was : Exit Do
                }
                p2 = (p - 1) / 2;
                if (OnCompare(p, p2) < 0)
                {
                    SwitchElements(p, p2);
                    p = p2;
                }
                else
                {
                    break; // TODO: might not be correct. Was : Exit Do
                }
            } while (true);
            return p;
        }

        /// <summary>
        /// Get the smallest object and remove it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Pop()
        {
            T result = InnerList[0];
            int p = 0;
            int p1 = 0;
            int p2 = 0;
            int pn = 0;
            InnerList[0] = InnerList[InnerList.Count - 1];
            InnerList.RemoveAt(InnerList.Count - 1);
            do
            {
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0)
                {
                    // links kleiner
                    p = p1;
                }
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0)
                {
                    // rechts noch kleiner
                    p = p2;
                }

                if (p == pn)
                {
                    break; // TODO: might not be correct. Was : Exit Do
                }
                SwitchElements(p, pn);
            } while (true);

            return result;
        }

        /// <summary>
        /// Notify the PQ that the object at position i has changed
        /// and the PQ needs to restore order.
        /// Since you dont have access to any indexes (except by using the
        /// explicit IList.this) you should not call this function without knowing exactly
        /// what you do.
        /// </summary>
        /// <param name="i">The index of the changed object.</param>
        public void Update(int i)
        {
            int p = i;
            int pn = 0;
            int p1 = 0;
            int p2 = 0;
            do
            {
                // aufsteigen
                if (p == 0)
                {
                    break; // TODO: might not be correct. Was : Exit Do
                }
                p2 = (p - 1) / 2;
                if (OnCompare(p, p2) < 0)
                {
                    SwitchElements(p, p2);
                    p = p2;
                }
                else
                {
                    break; // TODO: might not be correct. Was : Exit Do
                }
            } while (true);
            if (p < i)
            {
                return;
            }
            do
            {
                // absteigen
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0)
                {
                    // links kleiner
                    p = p1;
                }
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0)
                {
                    // rechts noch kleiner
                    p = p2;
                }

                if (p == pn)
                {
                    break; // TODO: might not be correct. Was : Exit Do
                }
                SwitchElements(p, pn);
            } while (true);
        }

        /// <summary>
        /// Get the smallest object without removing it.
        /// </summary>
        /// <returns>The smallest object</returns>
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

    /// <summary>
    /// 
    /// </summary>
    public class MovementPath
    {
        /// <summary>
        /// 
        /// </summary>
        public List<int> TransitCells
        {
            get;
            private set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<int> Directions
        {
            get;
            private set;
        }

        /// <summary>
        /// Cell count per straight segment, in segment order.
        /// </summary>
        public List<int> SegmentLengths
        {
            get;
            private set;
        }

        /// <summary>
        ///
        /// </summary>
        public int BeginCell => TransitCells.FirstOrDefault();

        /// <summary>
        ///
        /// </summary>
        public int MovementLength
        {
            get;
            set;
        }

        /// <summary>
        /// Sum of per-segment times, each computed with its own direction and speed.
        /// Walk speed is used for paths shorter than 4 cells; run speed for 4+.
        /// </summary>
        public double MovementTime
        {
            get
            {
                if (MovementLength <= 0 || SegmentLengths.Count == 0)
                    return 0;
                var speeds = MovementLength >= 4 ? Pathfinding.RUN_SPEEDS : Pathfinding.WALK_SPEEDS;
                double total = 0;
                for (int i = 0; i < SegmentLengths.Count; i++)
                {
                    int dir = i * 2 < Directions.Count ? Directions[i * 2] : (Directions.Count > 0 ? Directions[0] : 1);
                    total += speeds[dir] * 1100 * SegmentLengths[i];
                }
                return total;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int LastStep => TransitCells.Count == 0 ? -1 : TransitCells[TransitCells.Count < 2 ? 0 : TransitCells.Count - 2];

        /// <summary>
        /// 
        /// </summary>
        public int EndCell => TransitCells.LastOrDefault();

        /// <summary>
        /// 
        /// </summary>
        private StringBuilder m_serializedPath;
        
        /// <summary>
        /// 
        /// </summary>
        public MovementPath()
        {
            TransitCells = new List<int>();
            Directions = new List<int>();
            SegmentLengths = new List<int>();
        }
                   
        /// <summary>
        ///
        /// </summary>
        public void AddCell(int Cell, int Direction)
        {
            TransitCells.Add(Cell);
            Directions.Add(Direction);
        }

        /// <summary>
        /// Called once per validated segment with the number of cells traversed.
        /// </summary>
        public void AddSegmentLength(int segmentLength)
        {
            SegmentLengths.Add(segmentLength);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Cell"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (m_serializedPath == null)
            {
                m_serializedPath = new StringBuilder();
                for (int i = 0; i < TransitCells.Count; i++)
                {
                    m_serializedPath.Append(Pathfinding.GetDirectionChar((DirectionEnum)Directions[i]));
                    m_serializedPath.Append(Util.CellToChar(TransitCells[i]));
                }
            }
            return m_serializedPath.ToString();
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

    /// <summary>
    /// 
    /// </summary>
    public static class Pathfinding
    {
        private static ILogger Logger = LogManager.GetLogger(typeof(Pathfinding));

        public static double[] RUN_SPEEDS = { 1.700000E-001, 1.500000E-001, 1.500000E-001, 1.500000E-001, 1.700000E-001, 1.500000E-001, 1.500000E-001, 1.500000E-001 };
        public static double[] WALK_SPEEDS = { 7.000000E-002, 6.000000E-002, 6.000000E-002, 6.000000E-002, 7.000000E-002, 6.000000E-002, 6.000000E-002, 6.000000E-002 };
        public static double[] MOUNT_SPEEDS = { 2.300000E-001, 2.000000E-001, 2.000000E-001, 2.000000E-001, 2.300000E-001, 2.000000E-001, 2.000000E-001, 2.000000E-001 };

        private static FastRandom PATHFIND_RANDOM = new FastRandom();
        private static DirectionEnum[] FIGHT_DIRECTIONS = { DirectionEnum.Este, DirectionEnum.Sur, DirectionEnum.Oeste, DirectionEnum.Norte };

        public static bool IsValidCellId(MapInstance map, int cell)
        {
            return map != null && cell >= 0 && cell < map.Cells.Count;
        }

        // Keyed by map.Width (all maps of same width share the same direction offsets).
        private static ConcurrentDictionary<int, int[]> MapDirections = new ConcurrentDictionary<int, int[]>();
        // Keyed by cellsCount; value is a flat array indexed by cellId — faster than nested dicts.
        private static ConcurrentDictionary<int, Point[]> CellPoints = new ConcurrentDictionary<int, Point[]>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static double GetPathTime(int length, int direction)
        {
            return (length >= 4 ? RUN_SPEEDS[direction] : WALK_SPEEDS[direction]) * 1100 * length;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="beginCell"></param>
        /// <param name="encodedPath"></param>
        /// <returns></returns>
        public static int GetPathLength(MapInstance map, int beginCell, string encodedPath)
        {
            var lastCell = beginCell;
            var length = 0;

            for (int i = 0; i < encodedPath.Length; i += 3)
            {
                var actualCell = Util.CharToCell(encodedPath.Substring(i + 1, 2));
                length += GoalDistance(map, lastCell, actualCell);
                lastCell = actualCell;
            }

            return length;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="cellsCount"></param>
        public static void GenerateGrid(int width, int cellsCount)
        {
            var grid = new Point[cellsCount];
            for (int i = 0; i < cellsCount; i++)
                grid[i] = new Point(_GetX(width, i), _GetY(width, i));
            CellPoints.TryAdd(cellsCount, grid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        private static double _GetX(int width, int cell)
        {
            double loc5 = Math.Floor((double)(cell / (width * 2 - 1)));
            double loc6 = cell - loc5 * (width * 2 - 1);
            double loc7 = loc6 % width;

            return (cell - (width - 1) * (loc5 - loc7)) / width;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        private static double _GetY(int width, int cell)
        {
            double loc5 = Math.Floor((double)(cell / (width * 2 - 1)));
            double loc6 = cell - loc5 * (width * 2 - 1);
            double loc7 = loc6 % width;

            return loc5 - loc7;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="Cell"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static double GetX(MapInstance map, int cell)
        {
            if (!CellPoints.TryGetValue(map.Cells.Count, out var grid))
            {
                GenerateGrid(map.Width, map.Cells.Count);
                CellPoints.TryGetValue(map.Cells.Count, out grid);
            }
            return (grid != null && cell < grid.Length) ? grid[cell].X : -1000;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static double GetY(MapInstance map, int cell)
        {
            if (!CellPoints.TryGetValue(map.Cells.Count, out var grid))
            {
                GenerateGrid(map.Width, map.Cells.Count);
                CellPoints.TryGetValue(map.Cells.Count, out grid);
            }
            return (grid != null && cell < grid.Length) ? grid[cell].Y : -1000;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="beginCell"></param>
        /// <param name="endCell"></param>
        /// <returns></returns>
        public static bool InLine(MapInstance map, int beginCell, int endCell)
        {
            if (!IsValidCellId(map, beginCell) || !IsValidCellId(map, endCell))
                return false;

            var beginPoint = GetPoint(map, beginCell);
            var endPoint = GetPoint(map, endCell);
            
            return beginPoint.X == endPoint.X || beginPoint.Y == endPoint.Y;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="beginCell"></param>
        /// <param name="endCell"></param>
        /// <returns></returns>
        public static int GoalDistance(MapInstance map, int beginCell, int endCell)
        {
            if (!IsValidCellId(map, beginCell) || !IsValidCellId(map, endCell))
                return int.MaxValue;

            var beginPoint = GetPoint(map, beginCell);
            var endPoint = GetPoint(map, endCell);
            var distance = (int)(Math.Abs(endPoint.X - beginPoint.X) + Math.Abs(endPoint.Y - beginPoint.Y));
            
            return distance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static char GetDirectionChar(DirectionEnum direction)
        {
            return Util.HASH[(int)direction];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static int GetDirection(char direction)
        {
            return Util.HASH.IndexOf(direction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public static int[] GetDirectionChanges(MapInstance map)
        {
            if (MapDirections.TryGetValue(map.Width, out var cached))
                return cached;

            var directions = new int[]
            {
                1,
                map.Width,
                map.Width * 2 - 1,
                map.Width - 1, -1,
                -map.Width,
                -map.Width * 2 + 1,
                -(map.Width - 1)
            };

            MapDirections.TryAdd(map.Width, directions);
            return directions;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="beginCell"></param>
        /// <param name="dndCell"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="currentCell"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static MovementPath DecodePath(MapInstance map, int currentCell, string path)
        {
            MovementPath movementPath = new MovementPath();

            if (string.IsNullOrEmpty(path) || path.Length < 3 || path.Length % 3 != 0)
                return movementPath;

            var firstCell = Util.CharToCell(path.Substring(1, 2));
            if (GetDirection(path[0]) == -1 || map.GetCell(firstCell) == null)
                return movementPath;

            movementPath.AddCell(currentCell, (int)GetDirection(map, currentCell, firstCell));

            for (int i = 0; i < path.Length; i += 3)
            {
                int curCell = Util.CharToCell(path.Substring(i + 1, 2));
                int curDir = Util.HASH.IndexOf(path[i]);

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static DirectionEnum OppositeDirection(DirectionEnum direction)
        {
            return (DirectionEnum)((int)direction >= 4 ? (int)direction - 4 : (int)direction + 4);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="cellId"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="currentCell"></param>
        /// <param name="encodedPath"></param>
        /// <returns></returns>
        public static MovementPath IsValidPath(AbstractEntity entity, MapInstance map, int currentCell, string encodedPath)
        {
            var decodedPath = DecodePath(map, currentCell, encodedPath);
            if(decodedPath.TransitCells.Count < 2)
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

            if(entity.Type == EntityTypeEnum.TYPE_CHARACTER)
            {
                var mapCell = map.GetCell(decodedPath.EndCell);
                if(mapCell != null && mapCell.InteractiveObject != null && mapCell.InteractiveObject is Pheonix)
                {
                    var character = (CharacterEntity)entity;
                    character.AutomaticSkillId = (int)SkillIdEnum.SKILL_USE_PHOENIX;
                    character.AutomaticSkillCellId = decodedPath.EndCell;
                    character.AutomaticSkillMapId = map.Id;
                }
            }

            return finalPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fight"></param>
        /// <param name="fighter"></param>
        /// <param name="currentCell"></param>
        /// <param name="encodedPath"></param>
        /// <returns></returns>
        public static MovementPath IsValidPath(AbstractFight fight, AbstractFighter fighter, int currentCell, string encodedPath)
        {
            if (encodedPath == "")
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="beginCell"></param>
        /// <param name="direction"></param>
        /// <param name="endCell"></param>
        /// <returns></returns>
        public static int IsValidLine(AbstractEntity entity, MapInstance map, MovementPath finalPath, int beginCell, DirectionEnum direction, int endCell, int finalCell)
        {
            if (map.GetCell(beginCell) == null || map.GetCell(endCell) == null)
                return -1;

            var length = -1;
            var actualCell = beginCell;
            var lastCell = beginCell;

            finalPath.AddCell(actualCell, (int)direction);

            const int MAX_LOOP = 100;
            var time = 0;
            do
            {
                time++;
                if(time > MAX_LOOP)
                    return -1;

                actualCell = Pathfinding.NextCell(map, actualCell, direction);

                var mapCell = map.GetCell(actualCell);
                if (mapCell == null)
                {
                    length = -2;
                    break;
                }

                if (mapCell.InteractiveObject != null && (!mapCell.InteractiveObject.CanWalkThrough || (entity.Type == EntityTypeEnum.TYPE_CHARACTER && actualCell == finalCell && mapCell.InteractiveObject.IsActive)))
                {
                    length = -2;
                    break;
                }

                if (!mapCell.Walkable && !map.IsAnimatedDoorOpen(actualCell))
                {
                    length = -2;
                    break;
                }

                if (entity.Type == EntityTypeEnum.TYPE_CHARACTER && map.HasAggroNear((CharacterEntity)entity, lastCell))
                {
                    length = -2;
                    break;
                }

                length++;
                lastCell = actualCell;
                finalPath.MovementLength++;
            } while (actualCell != endCell);

            finalPath.AddCell(lastCell, (int)direction);

            if (length >= 0)
                finalPath.AddSegmentLength(length + 1);

            return length;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fight"></param>
        /// <param name="fighter"></param>
        /// <param name="path"></param>
        /// <param name="beginCell"></param>
        /// <param name="direction"></param>
        /// <param name="endCell"></param>
        /// <returns></returns>
        public static int IsValidLine(AbstractFight fight, AbstractFighter fighter, MovementPath path, int beginCell, DirectionEnum direction, int endCell)
        {
            if (!FIGHT_DIRECTIONS.Contains(direction) || fight.GetCell(beginCell) == null || fight.GetCell(endCell) == null)
                return -1;

            var length = -1;
            var actualCell = beginCell;

            if (!Pathfinding.InLine(fight.Map, beginCell, endCell))
                return length;

            length = (int)GoalDistance(fight.Map, beginCell, endCell);

            path.AddCell(actualCell, (int)direction);

            for (int i = 0; i < length; i++)
            {
                actualCell = Pathfinding.NextCell(fight.Map, actualCell, direction);

                if (!fight.Map.IsWalkable(actualCell))
                {
                    return -2;
                }

                if (fight.GetFighterOnCell(actualCell) != null)
                    return -2;

                path.AddCell(actualCell, (int)direction);
                path.MovementLength++;

                if (Pathfinding.IsStopCell(fighter.Fight, fighter.Team, actualCell))
                    return -2;
            }

            return length;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="fighter"></param>
        ///// <returns></returns>
        public static int TryTacle(AbstractFighter fighter)
        {
            var ennemies = Pathfinding.GetEnnemiesNear(fighter.Fight, fighter.Team, fighter.Cell.Id);

            if (!ennemies.Any() || ennemies.All(ennemy => ennemy.StateManager.HasState(FighterStateEnum.STATE_ROOTED)))
                return -1;

            return Pathfinding.TryTacle(fighter, ennemies);
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="fighter"></param>
        ///// <param name="nearestEnnemies"></param>
        ///// <returns></returns>
        private static int TryTacle(AbstractFighter fighter, IEnumerable<AbstractFighter> nearestEnnemies)
        {
            var fighterAgility = fighter.Statistics.GetTotal(EffectEnum.AddAgility);
            int ennemiesAgility = 0;

            foreach (var ennemy in nearestEnnemies)
                if (!ennemy.StateManager.HasState(FighterStateEnum.STATE_ROOTED))
                    ennemiesAgility += ennemy.Statistics.GetTotal(EffectEnum.AddAgility);

            var A = fighterAgility + 25;
            var B = fighterAgility + ennemiesAgility + 50;
            if (B == 0)
                B = 1;
            var chance = (int)((long)(300 * A / B) - 100);
            var rand = Pathfinding.PATHFIND_RANDOM.Next(0, 99);

            return rand > chance ? rand : -1;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="cellId"></param>
        ///// <returns></returns>
        public static bool IsStopCell(AbstractFight fight, FightTeam team, int cellId)
        {
            if (fight.GetCell(cellId).HasObject(FightObstacleTypeEnum.TYPE_TRAP))
                return true;

            if (GetEnnemiesNear(fight, team, cellId).Count() > 0)
                return true;

            return false;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="fight"></param>
        ///// <param name="team"></param>
        ///// <param name="cellId"></param>
        ///// <returns></returns>
        public static IEnumerable<AbstractFighter> GetEnnemiesNear(AbstractFight fight, FightTeam team, int cellId)
        {
            return GetFightersNear(fight, cellId).Where(fighter => fighter.Team != team);
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="fight"></param>
        ///// <param name="team"></param>
        ///// <param name="cellId"></param>
        ///// <returns></returns>
        public static List<AbstractFighter> GetFightersNear(AbstractFight fight, int cellId)
        {
            List<AbstractFighter> fighters = new List<AbstractFighter>();
            foreach (var direction in Pathfinding.FIGHT_DIRECTIONS)
            {
                var fighter = fight.GetFighterOnCell(Pathfinding.NextCell(fight.Map, cellId, direction));
                if (fighter != null)
                    if (!fighter.IsFighterDead)
                        fighters.Add(fighter);
            }
            return fighters;
        }

        // Swap the values of A and B
        private static void Swap<T>(ref T a, ref T b)
        {
            T c = a;
            a = b;
            b = c;
        }

        // Returns the list of points from p0 to p1 
        private static bool BresenhamLine(AbstractFight fight, int beginCell, int endCell)
        {
            if (beginCell == endCell)
                return true;

            var begin = GetPoint(fight.Map, beginCell);
            var end = GetPoint(fight.Map, endCell);
            return BresenhamLine(fight, beginCell, endCell, (int)begin.X, (int)begin.Y, (int)end.X, (int)end.Y);
        }

        private static bool BresenhamLine(AbstractFight fight, int beginCell, int endCell, int x0, int y0, int x1, int y1)
        {
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }
            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            int deltax = x1 - x0;
            int deltay = Math.Abs(y1 - y0);
            int error = 0;
            int ystep;
            int y = y0;
            if (y0 < y1) ystep = 1; else ystep = -1;
            for (int x = x0; x <= x1; x++)
            {
                int cellId = -1;
                if (steep)
                { 
                    cellId = GetCell(fight.Map, y, x);
                }
                else
                {
                    cellId = GetCell(fight.Map, x, y);
                }
                if (cellId != beginCell && cellId != endCell)
                {
                    var fightCell = fight.GetCell(cellId);
                    if (fightCell == null)
                        return false;
                    if (!fightCell.LineOfSight)
                        return false;
                    if (fightCell.HasObject(FightObstacleTypeEnum.TYPE_FIGHTER))
                        return false;
                }

                error += deltay;
                if (2 * error >= deltax)
                {
                    y += ystep;
                    error -= deltax;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fight"></param>
        /// <param name="beginCell"></param>
        /// <param name="endCell"></param>
        /// <returns></returns>
        public static bool CheckView(AbstractFight fight, int beginCell, int endCell)
        {
            return BresenhamLine(fight, beginCell, endCell);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int GetCell(MapInstance map, double x, double y)
        {
            return (int)x * map.Width + (int)y * (map.Width - 1);
        }

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

    /// <summary>
    /// 
    /// </summary>
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

            directions = new int[]
            {
                map.Width,
                map.Width - 1,
                -map.Width,
                -map.Width + 1,
                1,
                (map.Width * 2) - 1,
                -1,
                -((map.Width * 2) - 1)
            };

            // CalcGrid/OpenList/ClosedList initialized lazily on first FindPath call.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startCell"></param>
        /// <param name="endCell"></param>
        /// <param name="diagonal"></param>
        /// <param name="movementPoints"></param>
        /// <param name="obstacles"></param>
        /// <returns></returns>
        public string FindPathAsString(int startCell, int endCell, bool diagonal, int movementPoints = -1, IEnumerable<int> obstacles = null)
        {
            var pathList = FindPath(startCell, endCell, diagonal, movementPoints, obstacles == null ? new List<int>() : obstacles);

            var sb = new StringBuilder();
            for (int i = 0; i <= pathList.Count - 2; i++)
            {
                sb.Append(Pathfinding.GetDirectionChar(Pathfinding.GetDirection(map, pathList[i], pathList[i + 1])));
                sb.Append(Util.CellToChar(pathList[i + 1]));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="StartCell"></param>
        /// <param name="EndCell"></param>
        /// <param name="Diagonal"></param>
        /// <param name="MovementPoints"></param>
        /// <param name="Obstacles"></param>
        /// <returns></returns>
        private List<int> FindPath(int StartCell, int EndCell, bool Diagonal, int MovementPoints = -1, IEnumerable<int> Obstacles = null)
        {
            var blockedCells = Obstacles == null
                ? new HashSet<int>()
                : Obstacles.Where(cell => Pathfinding.IsValidCellId(map, cell)).ToHashSet();

            if (!Pathfinding.IsValidCellId(map, StartCell) || !Pathfinding.IsValidCellId(map, EndCell))
                return new List<int>();

            if (StartCell == EndCell)
                return new List<int> { StartCell };

            if (MovementPoints == 0)
                return new List<int>();

            if (CalcGrid == null)
            {
                CalcGrid  = new PathNode[cellCount + 1];
                OpenList  = new PriorityQueueB<int>(new ComparePFNodeMatrix(CalcGrid));
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

                    double NewG = CalcGrid[Location].G + 1;

                    if ((CalcGrid[NewLocation].Status == NodeState.InOpenList || CalcGrid[NewLocation].Status == NodeState.InCloseList)
                        && CalcGrid[NewLocation].G <= NewG)
                        continue;

                    Point NewLocationPoint = Pathfinding.GetPoint(map, NewLocation);

                    CalcGrid[NewLocation].Cell = NewLocation;
                    CalcGrid[NewLocation].Parent = Location;
                    CalcGrid[NewLocation].G = NewG;

                    double H = Math.Abs(NewLocationPoint.X - EndPoint.X) + Math.Abs(NewLocationPoint.Y - EndPoint.Y);

                    double Cross = Math.Abs(
                        (NewLocationPoint.X - EndPoint.X) * (StartPoint.Y - EndPoint.Y) -
                        (StartPoint.X - EndPoint.X) * (NewLocationPoint.Y - EndPoint.Y));

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

            int take = (MovementPoints > 0 && ClosedList.Count - 1 >= MovementPoints)
                ? MovementPoints + 1
                : ClosedList.Count;

            var result = new List<int>(take);
            for (int i = 0; i < take; i++)
                result.Add(ClosedList[i].Cell);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        internal class ComparePFNodeMatrix : IComparer<int>
        {
            /// <summary>
            /// 
            /// </summary>
            private PathNode[] mMatrix;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="matrix"></param>
            public ComparePFNodeMatrix(PathNode[] matrix)
            {
                mMatrix = matrix;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
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


