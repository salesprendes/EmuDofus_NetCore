using Game.Area;
using Game.Database.Repository;
using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Manager;
using Game.Network;
using Game.Action;
using Game.Spawn;
using Game.Database.Structure;
using Game.Interactive;
using Game.Interactive.Type;
using System.Threading;
using Game.Mount;

namespace Game.Map
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MapInstance : MessageDispatcher, IMovementHandler, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private static string HASH_CELL = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

        private const int DOOR_OPEN_MOVEMENT = 4;
        private const string CELL_MOVEMENT_MASK = "801";

        // O(1) map-cell char decode — built once, shared across all MapInstance objects
        private static readonly int[] s_hashCellIndex = BuildHashCellIndex();
        private static int[] BuildHashCellIndex()
        {
            var idx = new int[128];
            for (int i = 0; i < idx.Length; i++) idx[i] = -1;
            for (int i = 0; i < HASH_CELL.Length; i++) idx[HASH_CELL[i]] = i;
            return idx;
        }
        

        /// <summary>
        /// 
        /// </summary>
        private static long m_NextMonsterId;

        /// <summary>
        /// 
        /// </summary>
        private sealed class DoorAnimationDefinition
        {
            public int CellId { get; private set; }
            public int OpeningDuration { get; private set; }
            public int OpenedDuration { get; private set; }
            public int ClosingDuration { get; private set; }

            public DoorAnimationDefinition(int cellId, int openingDuration, int openedDuration, int closingDuration)
            {
                CellId = cellId;
                OpeningDuration = openingDuration;
                OpenedDuration = openedDuration;
                ClosingDuration = closingDuration;
            }

            public AnimatedDoor Create(MapInstance map)
            {
                return new AnimatedDoor(map, CellId, OpeningDuration, OpenedDuration, ClosingDuration);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class DoorSwitchDefinition
        {
            public int DoorCellId { get; private set; }
            public int[] TriggerCellIds { get; private set; }
            public int RequiredPlayers { get; private set; }
            public int OpenedDuration { get; private set; }

            public DoorSwitchDefinition(int doorCellId, int[] triggerCellIds, int requiredPlayers, int openedDuration)
            {
                DoorCellId = doorCellId;
                TriggerCellIds = triggerCellIds;
                RequiredPlayers = requiredPlayers;
                OpenedDuration = openedDuration;
            }

            public bool HasTriggerCell(int cellId)
            {
                return TriggerCellIds.Contains(cellId);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static readonly Dictionary<int, DoorAnimationDefinition[]> s_doorAnimationsByMap = new Dictionary<int, DoorAnimationDefinition[]>
        {
            { 736,   new[] { new DoorAnimationDefinition(224, 4700, 50000, 3700) } },
            { 8538,  new[] { new DoorAnimationDefinition(125, 4700, 25000, 3700) } },
            { 10352, new[] { new DoorAnimationDefinition(98,  3333, 30000, 3700) } },
            // Pandala conquest gates (outer) — ForceOpen/ForceClose bypass timers; durations are irrelevant for conquest use
            { 8214,  new[] { new DoorAnimationDefinition(403, 500, -1, 500), new DoorAnimationDefinition(373, 500, -1, 500) } }, // Akwadala gate (confirmed via packet)
            { 7951,  new[] { new DoorAnimationDefinition(323, 500, -1, 500), new DoorAnimationDefinition(295, 500, -1, 500) } }, // Aerdala gate  (confirmed via packet)
            { 7896,  new[] { new DoorAnimationDefinition(284, 500, -1, 500), new DoorAnimationDefinition(325, 500, -1, 500) } }, // Feudala gate  (TODO: verify cells)
            { 8268,  new[] { new DoorAnimationDefinition(307, 500, -1, 500), new DoorAnimationDefinition(353, 500, -1, 500) } }, // Terrdala gate (TODO: verify cells)
            // Pandala prism building doors (inner) — open when neutral, closed when conquered
            { 8346,  new[] { new DoorAnimationDefinition(141, 500, -1, 500) } },                                                 // Feudala  inner (cell 141 → trigger at 156)
            { 8076,  new[] { new DoorAnimationDefinition(106, 500, -1, 500), new DoorAnimationDefinition(108, 500, -1, 500) } }, // Terrdala inner (TODO: verify cells near trigger 122)
            { 8137,  new[] { new DoorAnimationDefinition(270, 500, -1, 500) } },                                                 // Akwadala inner (cell 270 — confirmed via packet)
            // { 8153, new[] { ... } },  // Aerdala  inner — door cell unknown, needs packet capture
        };

        /// <summary>
        /// 
        /// </summary>
        private static readonly Dictionary<int, DoorSwitchDefinition[]> s_doorSwitchesByMap = new Dictionary<int, DoorSwitchDefinition[]>
        {
            { 736, new[] { new DoorSwitchDefinition(224, new[] { 260 }, 1, 50000) } },
            { 8538, new[] { new DoorSwitchDefinition(125, new[] { 88 }, 1, 25000) } },
            { 10352, new[] { new DoorSwitchDefinition(98, new[] { 299, 327, 355 }, 1, 30000) } },
        };

        /// <summary>
        /// 
        /// </summary>
        private static long NextMonsterId => Interlocked.Decrement(ref m_NextMonsterId);

        /// <summary>
        /// 
        /// </summary>
        public FieldTypeEnum FieldType => FieldTypeEnum.TYPE_MAP;

        public Pathmaker Pathmaker
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public FightManager FightManager
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Id
        {
            get;
            private set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public int SubAreaId
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int X
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Y
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Width
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Height
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Data
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string DataKey
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string CreateTime
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<int> FightTeam0Cells
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<int> FightTeam1Cells
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public SubAreaInstance SubArea
        {
            get
            {
                if (m_subArea == null)
                    m_subArea = AreaManager.Instance.GetSubArea(SubAreaId);
                return m_subArea;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<AbstractEntity> Entities => m_entityById.Values;

        public TaxCollectorEntity TaxCollector => m_taxCollector;

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<MapCell> Cells => m_cellsArray;

        /// <summary>
        /// 
        /// </summary>
        public bool CanAbortMovement => true;

        /// <summary>
        /// 
        /// </summary>
        public int RandomTeleportCell
        {
            get
            {
                var actionCell = Array.Find(m_cellsArray, cell => cell.Trigger != null);

                if (actionCell != null)
                    return actionCell.Id;

                actionCell = Array.Find(m_cellsArray, cell => cell.Walkable);
                if (actionCell != null)
                    return actionCell.Id;

                return -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int RandomFreeCell
        {
            get
            {
                var candidates = m_walkableCellIds.Where(id => m_cellsArray[id].Walkable && !m_occupiedCells.Contains(id)).ToList();

                if (candidates.Count > 0)
                    return candidates[Util.Next(0, candidates.Count)];

                if (m_walkableCellIds.Length > 0)
                    return m_walkableCellIds[Util.Next(0, m_walkableCellIds.Length)];

                return -1;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public int PlayerCount => m_playerCount;

        /// <summary>
        /// True once InitializeOnFirstPlayerEnter has run (NPCs/monsters already seeded).
        /// </summary>
        public bool IsInitialized => m_initialized;

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<InteractiveObject> InteractiveObjects => m_interactiveObjects;

        public Paddock Paddock => m_paddock;

        /// <summary>
        /// 
        /// </summary>       
        private Dictionary<long, AbstractEntity> m_entityById;
        private Dictionary<string, AbstractEntity> m_entityByName;
        private Dictionary<int, AnimatedDoor> m_animatedDoorByCellId;
        private Dictionary<int, (string closed, string open)> m_doorCellEncodings;
        private MapCell[] m_cellsArray;
        private int[] m_walkableCellIds;
        private List<InteractiveObject> m_interactiveObjects;
        private SubAreaInstance m_subArea;
        private Paddock m_paddock;
        private bool m_subInstance;
        private int m_playerCount;
        private bool m_initialized;
        private bool m_interactiveObjectsRegistered;
        private SpawnQueue m_spawnQueue;
        private List<MonsterSpawnDAO> m_monsters;
        private int m_spawnCounter;
        // Pre-built entity lists — maintained in SpawnEntity/DestroyEntity, no LINQ on hot paths
        private List<AbstractEntity> m_moveableEntities;
        private List<MonsterGroupEntity> m_monsterGroups;
        private TaxCollectorEntity m_taxCollector;
        private ConquestPrismEntity m_conquestPrism;
        private HashSet<int> m_occupiedCells;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subArea"></param>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="data"></param>
        /// <param name="dataKey"></param>
        /// <param name="createTime"></param>
        public MapInstance(int subAreaId, int id, int x, int y, int width, int height, string data, string dataKey, string createTime, List<int> f0teamCells, List<int> f1teamCells, bool subInstance = false)
        {
            Id = id;
            SubAreaId = subAreaId;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Data = data;
            DataKey = dataKey;
            CreateTime = createTime;
            FightTeam0Cells = f0teamCells;
            FightTeam1Cells = f1teamCells;

            m_subInstance = subInstance;
            m_interactiveObjects = new List<InteractiveObject>();
            m_animatedDoorByCellId = new Dictionary<int, AnimatedDoor>();
            m_entityById = new Dictionary<long, AbstractEntity>();
            m_entityByName = new Dictionary<string, AbstractEntity>();
            m_moveableEntities = new List<AbstractEntity>();
            m_monsterGroups = new List<MonsterGroupEntity>();
            m_occupiedCells = new HashSet<int>();
            m_initialized = false;

            m_paddock = PaddockManager.Instance.GetByMapId(Id);

            FightManager = new FightManager(this);
            SubArea.AddUpdatable(this);

            if (!m_subInstance)
            {
                // Main maps: full broadcast + monster spawns
                SubArea.SafeAddHandler(base.Dispatch);
                SpawnManager.Instance.RegisterMap(this);
            }
            // Sub-instances are isolated: no cross-map broadcast, spawns registered by MapManager

            Initialize();
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void Initialize()
        {
            var triggers = MapTriggerRepository.Instance.GetTriggers(Id);

            // Pre-index triggers by CellId for O(1) lookup instead of O(n) Find per cell
            Dictionary<int, MapTriggerDAO> triggerByCellId = null;
            if (triggers.Count > 0)
            {
                triggerByCellId = new Dictionary<int, MapTriggerDAO>(triggers.Count);
                foreach (var t in triggers)
                    if (!triggerByCellId.ContainsKey(t.CellId))
                        triggerByCellId.Add(t.CellId, t);
            }

            int cellCount = Data.Length / 10;
            m_cellsArray = new MapCell[cellCount];
            var walkableIds = new List<int>();

            // Temp buffer of raw decoded bytes — kept only during Initialize() to pre-encode
            // door cell strings. Discarded after InitializeDoorAnimations(). The 10 raw bytes
            // per cell are no longer retained in MapCell itself.
            var rawBytes = new byte[cellCount * 10];
            var cellData = new byte[10];

            for (int i = 0; i < Data.Length; i += 10)
            {
                var id = i / 10;
                for (int j = 0; j < 10; j++)
                {
                    cellData[j] = (byte)s_hashCellIndex[Data[i + j]];
                    rawBytes[id * 10 + j] = cellData[j];
                }

                MapTriggerDAO trigger = null;
                triggerByCellId?.TryGetValue(id, out trigger);

                var cell = new MapCell(this, id, cellData, trigger);
                if (cell.InteractiveObject != null)
                {
                    m_interactiveObjects.Add(cell.InteractiveObject);

                    var door = cell.InteractiveObject as AnimatedDoor;
                    if (door != null)
                        m_animatedDoorByCellId[id] = door;
                }
                m_cellsArray[id] = cell;
                if (cell.Walkable) walkableIds.Add(id);
            }

            m_walkableCellIds = walkableIds.ToArray();

            InitializeDoorAnimations();

            // Pre-encode closed/open strings for every animated door cell.
            // Only these cells ever need EncodeData; all others no longer store raw bytes.
            if (m_animatedDoorByCellId.Count > 0)
            {
                m_doorCellEncodings = new Dictionary<int, (string closed, string open)>(m_animatedDoorByCellId.Count);
                foreach (var cellId in m_animatedDoorByCellId.Keys)
                {
                    if (cellId < cellCount)
                        m_doorCellEncodings[cellId] = (
                            closed: EncodeCellBytes(rawBytes, cellId * 10),
                            open:   EncodeCellBytesWithMovement(rawBytes, cellId * 10, DOOR_OPEN_MOVEMENT)
                        );
                }
            }
            // rawBytes goes out of scope here and is collected by GC

            Pathmaker = new Pathmaker(this);
        }

        private static string EncodeCellBytes(byte[] raw, int offset)
        {
            var chars = new char[10];
            for (int i = 0; i < 10; i++)
                chars[i] = HASH_CELL[raw[offset + i]];
            return new string(chars);
        }

        private static string EncodeCellBytesWithMovement(byte[] raw, int offset, int movement)
        {
            var chars = new char[10];
            for (int i = 0; i < 10; i++)
                chars[i] = HASH_CELL[i == 2 ? (byte)((raw[offset + 2] & ~56) | ((movement & 7) << 3)) : raw[offset + i]];
            return new string(chars);
        }

        /// <summary>
        ///
        /// </summary>
        private void InitializeDoorAnimations()
        {
            DoorAnimationDefinition[] definitions;
            if (!s_doorAnimationsByMap.TryGetValue(Id, out definitions))
                return;

            foreach (var definition in definitions)
            {
                if (definition.CellId >= m_cellsArray.Length || m_animatedDoorByCellId.ContainsKey(definition.CellId))
                    continue;

                var door = definition.Create(this);
                m_animatedDoorByCellId.Add(definition.CellId, door);
                m_interactiveObjects.Add(door);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public MapInstance Clone()
        {
            return new MapInstance(SubAreaId, Id, X, Y, Width, Height, Data, DataKey, CreateTime, FightTeam0Cells, FightTeam1Cells, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spawnQueue"></param>
        public void SetSpawnQueue(SpawnQueue spawnQueue)
        {
            m_spawnQueue = spawnQueue;
        }

        /// <summary>
        ///
        /// </summary>
        private void RegisterAllInteractiveObjects()
        {
            if (m_interactiveObjectsRegistered)
                return;
            m_interactiveObjectsRegistered = true;
            foreach (var obj in m_interactiveObjects)
            {
                AddUpdatable(obj);
                obj.AddHandler(Dispatch);
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void InitializeOnFirstPlayerEnter()
        {
            if (m_initialized)
                return;
            m_initialized = true;
            RegisterAllInteractiveObjects();
            UpdateConquestDoors();
            InitPrismSpawn();
            InitNpcsSpawn();
            InitMonstersSpawn();
            InitEntitiesMovements();
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitNpcsSpawn()
        {
            int nextNpcId = 1;
            foreach (var npc in NpcManager.Instance.GetByMapId(Id))
                SpawnEntity(new NonPlayerCharacterEntity(npc, nextNpcId++));
        }

        // True when this is a conquest-tagged subarea (CanConquest=1) inside a village
        // city area that currently has no active territory/prism.
        // Guards must not spawn until the city is conquered by an alignment.
        private bool IsConquestVillageWithoutTerritory()
        {
            if (!SubArea.CanConquest) return false;
            var area = SubArea.Area;
            return area != null
                && ConquestManager.IsVillageArea(area.Id)
                && !ConquestManager.Instance.IsVillageAreaConquered(area.Id);
        }

        // Opens animated doors when the city is neutral, closes them when conquered.
        // Called on first player enter and whenever territory state changes.
        private void UpdateConquestDoors()
        {
            if (m_animatedDoorByCellId.Count == 0) return;
            var area = SubArea.Area;
            if (area == null || !ConquestManager.IsVillageArea(area.Id)) return;

            bool neutral = !ConquestManager.Instance.IsVillageAreaConquered(area.Id);
            foreach (var door in m_animatedDoorByCellId.Values)
            {
                if (neutral) door.ForceOpen();
                else         door.ForceClose();
            }
        }

        private void InitPrismSpawn()
        {
            UpdateConquestPrism();
        }

        private void SpawnConquestPrism(ConquestPrismEntity prism)
        {
            SpawnEntity(prism);
        }

        private void UpdateConquestPrism()
        {
            if (m_conquestPrism != null && m_conquestPrism.HasGameAction(GameActionTypeEnum.FIGHT))
                return;

            var prism = ConquestManager.Instance.CreatePrismEntityForMap(this);

            if (m_conquestPrism != null)
            {
                if (prism != null && m_conquestPrism.Represents(prism.Territory, prism.MapId, prism.MapCellId))
                {
                    prism.Dispose();
                    return;
                }

                DestroyEntity(m_conquestPrism);
            }

            if (prism != null)
                SpawnConquestPrism(prism);
        }

        public void ScheduleConquestDoorUpdate()
        {
            AddMessage(() =>
            {
                UpdateConquestDoors();
                if (m_initialized)
                    UpdateConquestPrism();
            });
        }

        /// <summary>
        ///
        /// </summary>
        private void InitMonstersSpawn()
        {
            m_monsters = new List<MonsterSpawnDAO>(MonsterSpawnRepository.Instance.GetById(ZoneTypeEnum.TYPE_MAP, Id).OrderByDescending(spawn => spawn.Probability));

            if (IsConquestVillageWithoutTerritory())
                return;

            m_spawnCounter = m_monsters.Count > 0 ? WorldConfig.SPAWN_MAX_GROUP_PER_MAP : 0;

            while (m_spawnCounter > 0)
                SpawnMonsters();
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitEntitiesMovements()
        {
            AddTimer(5000, ProcessEntitiesMovements);
        }

        private void ProcessEntitiesMovements()
        {
            // Dormant: no players watching — skip all entity ticking (Ankama-style idle suppression)
            if (m_playerCount == 0) return;
            for (int i = 0; i < m_moveableEntities.Count; i++)
                MoveEntity(m_moveableEntities[i]);
        }

        // Spread entity first-move times evenly across their interval window so they don't
        // all fire at once when the first player enters a previously empty map.
        private void StaggerEntityMovements()
        {
            int count = m_moveableEntities.Count;
            if (count == 0) return;
            for (int i = 0; i < count; i++)
            {
                var entity = m_moveableEntities[i];
                if (entity.MovementInterval == 0)
                    entity.MovementInterval = Util.Next(10000, 25000);
                entity.NextMovementTime = UpdateTime + (long)entity.MovementInterval * i / count;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void MoveEntity(AbstractEntity entity)
        {
            if (entity.MovementInterval == 0)
                entity.MovementInterval = Util.Next(10000, 25000);

            if(entity.NextMovementTime == 0)            
                entity.NextMovementTime = UpdateTime + entity.MovementInterval;
            
            if (entity.NextMovementTime > UpdateTime)
                return;

            entity.NextMovementTime = UpdateTime + entity.MovementInterval;

            // Move only if there is a player on the map, else it is useless
            if (m_playerCount == 0)
                return;

            var cellId = entity.LastCellId;
            if (cellId < 1)
                cellId = GetNearestMovementCell(entity.CellId);

            if (entity.LastCellId == 0)
                entity.LastCellId = entity.CellId;
            else
                entity.LastCellId = 0;

            if (cellId < 1)
                return;

            Move(entity, entity.CellId, Pathmaker.FindPathAsString(entity.CellId, cellId, false));

            entity.StopAction(GameActionTypeEnum.MAP_MOVEMENT);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MapCell GetCell(int id)
        {
            if (id >= 0 && id < m_cellsArray.Length)
                return m_cellsArray[id];
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public int GetNearestCell(int cellId)
        {
            foreach(var nextCell in CellZone.GetAdjacentCells(this, cellId))
            {
                var cell = GetCell(nextCell);
                if (cell != null && cell.Walkable)
                    return nextCell;
            }
            return -1;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public int GetNearestMovementCell(int cellId)
        {
            var rand = Util.Next(0, 101);
            var direction = DirectionEnum.Este;
            if (rand < 25)
                direction = DirectionEnum.Sur;
            else if (rand < 50)
                direction = DirectionEnum.Oeste;
            else if (rand < 75)
                direction = DirectionEnum.Norte;

            var nextCellId = Pathfinding.NextCell(this, cellId, direction);
            var cell = GetCell(nextCellId);
            if(cell != null && cell.Walkable)
                if (cell.Walkable)
                    return nextCellId;
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public bool IsWalkable(int cellId)
        {
            MapCell cell = GetCell(cellId);
            if (cell != null)
                return cell.Walkable || IsAnimatedDoorOpen(cellId);
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public bool IsAnimatedDoorOpen(int cellId)
        {
            AnimatedDoor door;
            return m_animatedDoorByCellId.TryGetValue(cellId, out door) && door.IsOpened;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <param name="opened"></param>
        public void SendAnimatedDoorCellState(int cellId, bool opened)
        {
            var message = BuildDoorCellStateMessage(cellId, opened);
            if (message != null)
                Dispatch(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <param name="opened"></param>
        /// <returns></returns>
        private string BuildDoorCellStateMessage(int cellId, bool opened)
        {
            if (m_doorCellEncodings == null || !m_doorCellEncodings.TryGetValue(cellId, out var encodings))
                return null;
            string data = opened ? encodings.open : encodings.closed;
            return WorldMessage.GAME_DATA_CELL(cellId, data, CELL_MOVEMENT_MASK);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AbstractEntity GetEntity(long id)
        {
            if (m_entityById.ContainsKey(id))
                return m_entityById[id];
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SpawnMonsters()
        {
            if (m_monsters.Count > 0 && FightTeam1Cells.Count > 0)
            {
                var cellId = RandomFreeCell;
                if (cellId >= 0)
                {
                    var group = new MonsterGroupEntity(NextMonsterId, Id, cellId, m_monsters, FightTeam1Cells.Count);
                    if (group.HasMonsters)
                        SpawnEntity(group);
                }
            }
            m_spawnCounter--;
        }

        /// <summary>
        ///
        /// </summary>
        public void SpawnMonsters(IEnumerable<MonsterSpawnDAO> monsters)
        {
            if (IsConquestVillageWithoutTerritory())
                return;

            if(monsters.Any())
            {
                var cellId = RandomFreeCell;
                if (cellId >= 0)
                {
                    var group = new MonsterGroupEntity(NextMonsterId, Id, cellId, monsters, FightTeam1Cells.Count);
                    if (group.HasMonsters)
                        SpawnEntity(group);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="grades"></param>
        public bool StartMonsterFight(CharacterEntity character, IEnumerable<MonsterGradeDAO> grades)
        {
            var cellId = RandomFreeCell;
            if (cellId < 0)
                return false;

            var group = new MonsterGroupEntity(NextMonsterId, Id, cellId, grades);
            return group.HasMonsters && FightManager.StartMonsterFight(character, group);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void SpawnEntity(AbstractEntity entity)
        {
            AddMessage(() =>
            {
                if (!m_entityById.ContainsKey(entity.Id))
                {
                    m_entityById.Add(entity.Id, entity);
                    m_occupiedCells.Add(entity.CellId);

                    // Maintain pre-built lists so hot paths skip LINQ
                    if (entity.CanBeMoved()) m_moveableEntities.Add(entity);
                    if (entity is MonsterGroupEntity mg) m_monsterGroups.Add(mg);
                    if (entity is TaxCollectorEntity tc) m_taxCollector = tc;
                    if (entity is ConquestPrismEntity cp) m_conquestPrism = cp;

                    if (m_subInstance) // For npc etc
                        entity.SetMap(this);

                    Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_ADD, entity));
                    AddUpdatable(entity);

                    if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                    {
                        InitializeOnFirstPlayerEnter();

                        m_playerCount++;
                        m_entityByName.Add(entity.Name.ToLower(), entity);

                        // First player entering a dormant map: stagger entity move times
                        if (m_playerCount == 1) StaggerEntityMovements();

                        AddHandler(entity.Dispatch);
                        SendAllInformations(entity);
                    }
                }
                else
                {
                    Logger.Error("MapInstance::SpawnEntity : an entity with the same id alrezdy exists : " + entity.Name);

                    WorldService.Instance.AddUpdatable(entity);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void SendAllInformations(AbstractEntity entity)
        {
            entity.CachedBuffer = true;

            // Before showing up we span all required base entities
            SendMapInformations(entity);
            SendInteractiveData(entity);
            SendPaddockInformations(entity);
            entity.Dispatch(WorldMessage.GAME_DATA_SUCCESS());
            SendAnimatedDoorRuntimeStates(entity);

            // Sub data that arent necessary to be instantly shown
            SendFightCount(entity);
            SendFightsInformations(entity);

            entity.CachedBuffer = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void SendFightsInformations(AbstractEntity entity)
        {
            foreach (var fight in FightManager.Fights)
                fight.SendMapFightInfos(entity);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void SendFightCount(AbstractEntity entity) 
            => entity.Dispatch(WorldMessage.FIGHT_COUNT(FightManager.FightCount));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void SendMapInformations(AbstractEntity entity) 
            => entity.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_ADD, Entities.ToArray()));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void SendInteractiveData(AbstractEntity entity)
        {
            entity.Dispatch(WorldMessage.INTERACTIVE_DATA_FRAME(m_interactiveObjects));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        private void SendAnimatedDoorRuntimeStates(AbstractEntity entity)
        {
            foreach (var door in m_animatedDoorByCellId.Values)
            {
                if (door.IsClosed)
                    continue;

                if (door.IsOpened)
                {
                    var message = BuildDoorCellStateMessage(door.CellId, true);
                    if (message != null)
                        entity.Dispatch(message);
                }

                door.SendUpdateTo(entity);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void SendPaddockInformations(AbstractEntity entity)
        {
            if (m_paddock != null)
                m_paddock.SendInformations(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void DestroyEntity(AbstractEntity entity)
        {
            if (m_entityById.ContainsKey(entity.Id))
            {
                m_entityById.Remove(entity.Id);
                m_occupiedCells.Remove(entity.CellId);

                // Keep pre-built lists in sync
                if (entity.CanBeMoved()) m_moveableEntities.Remove(entity);
                if (entity is MonsterGroupEntity mg) m_monsterGroups.Remove(mg);
                if (entity is TaxCollectorEntity) m_taxCollector = null;
                if (entity is ConquestPrismEntity) m_conquestPrism = null;

                RemoveUpdatable(entity);
                Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_REMOVE, entity));

                if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                {
                    RemoveHandler(entity.Dispatch);

                    m_entityByName.Remove(entity.Name.ToLower());
                    m_playerCount--;

                    // Multiple instance released
                    if(m_playerCount == 0 && m_subInstance)
                    {
                        MapManager.Instance.ReleaseInstance(this);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="cellId"></param>
        /// <param name="skillId"></param>
        public void InteractiveExecute(CharacterEntity character, int cellId, int skillId)
        {
            var cell = GetCell(cellId);
            if(cell != null)
            {
                if(cell.InteractiveObject != null)
                {
                    var skill = character.CharacterJobs.GetSkill(skillId);
                    if (skill == null && !(cell.InteractiveObject is Pheonix))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    cell.InteractiveObject.UseWithSkill(character, skill);
                }
                else
                {
                    character.Dispatch(WorldMessage.SERVER_INFO_MESSAGE("Not implemented yet."));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cellId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public MovementPath DecodeMovement(AbstractEntity entity, int cellId, string path)
        {
            return Pathfinding.IsValidPath(entity, this, cellId, path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cellId"></param>
        /// <param name="movementPath"></param>
        public void Move(AbstractEntity entity, int cellId, string movementPath)
        {
            AddMessage(() =>
                {
                    var path = DecodeMovement(entity, cellId, movementPath);
                    if (path != null && path.MovementLength > 0)
                    {
                        entity.Move(path);
                    }
                    else if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                    {
                        var character = (CharacterEntity)entity;

                        // Informar al cliente que el movimiento falló para que no quede bloqueado
                        // esperando un GA de confirmación que nunca llegará.
                        character.Dispatch(WorldMessage.GAME_ACTION_FAILED());

                        // Si el path fue bloqueado por un IO adyacente (ej: Fénix a distancia 0),
                        // ejecutar el skill automático inmediatamente en lugar de esperar el fin del movimiento.
                        if (character.AutomaticSkillId != -1)
                        {
                            var skillId = character.AutomaticSkillId;
                            var skillCellId = character.AutomaticSkillCellId;
                            var skillMapId = character.AutomaticSkillMapId;
                            character.AutomaticSkillId = -1;
                            character.AutomaticSkillCellId = -1;
                            character.AutomaticSkillMapId = -1;
                            if (character.MapId == skillMapId)
                                InteractiveExecute(character, skillCellId, skillId);
                        }
                    }
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="cellId"></param>
        /// <param name="monsters"></param>
        /// <returns></returns>
        public bool CanBeAggro(CharacterEntity character, int cellId, MonsterGroupEntity monsters) => Pathfinding.GoalDistance(this, cellId, monsters.CellId) <= monsters.AggressionRange
                && ((character.AlignmentId == (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL && monsters.AlignmentId == -1)
                || (character.AlignmentId != (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL && monsters.AlignmentId != character.AlignmentId));

        public bool HasAggroNear(CharacterEntity character, int cellId)
        {
            for (int i = 0; i < m_monsterGroups.Count; i++)
            {
                if (CanBeAggro(character, cellId, m_monsterGroups[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        private int OpenDoorForPassage(int cellId)
        {
            AnimatedDoor door;
            if (!m_animatedDoorByCellId.TryGetValue(cellId, out door))
                return 0;

            return door.OpenTemporarily();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="cell"></param>
        /// <param name="cellId"></param>
        private void ApplyTriggerActions(CharacterEntity character, MapCell cell, int cellId)
        {
            var delay = OpenDoorForPassage(cellId);
            if (delay <= 0)
            {
                cell.ApplyActions(character);
                return;
            }

            AddTimer(delay, () =>
            {
                if (character.MapId == Id && character.CellId == cellId)
                    cell.ApplyActions(character);
            }, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="cellId"></param>
        private void TryOpenDoorSwitch(CharacterEntity character, int cellId)
        {
            DoorSwitchDefinition[] definitions;
            if (!s_doorSwitchesByMap.TryGetValue(Id, out definitions))
                return;

            foreach (var definition in definitions)
            {
                if (!definition.HasTriggerCell(cellId) || CountOccupiedSwitchCells(definition.TriggerCellIds) < definition.RequiredPlayers)
                    continue;

            AnimatedDoor door;
                if (m_animatedDoorByCellId.TryGetValue(definition.DoorCellId, out door))
                    door.OpenTemporarily(definition.OpenedDuration);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellIds"></param>
        /// <returns></returns>
        private int CountOccupiedSwitchCells(int[] cellIds)
        {
            var count = 0;
            foreach (var cellId in cellIds)
            {
                if (m_entityById.Values.OfType<CharacterEntity>().Any(character => character.CellId == cellId))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="path"></param>
        /// <param name="cellId"></param>
        public void MovementFinish(AbstractEntity entity, MovementPath path, int cellId)
        {
            if (entity.CellId == cellId)
                return;

            if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
            {
                var character = (CharacterEntity)entity;

                if (character.CanGameAction(GameActionTypeEnum.FIGHT))
                {
                    foreach (var monsterGroup in m_monsterGroups)
                    {
                        if (CanBeAggro(character, cellId, monsterGroup))
                        {
                            entity.CellId = cellId;
                            if (monsterGroup.AlignmentId == -1)
                            {
                                if (FightManager.StartMonsterFight(character, monsterGroup))
                                    return;
                            }
                            else
                            {
                                if (FightManager.StartAggression(monsterGroup, character))
                                    return;
                            }
                        }
                    }
                }
            }

            entity.Orientation = path.GetDirection(path.LastStep);

            if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
            {
                var character = (CharacterEntity)entity;
                var cell = GetCell(cellId);
                if (cell != null)
                {
                    if (cell.Trigger != null)
                    {
                        if(!cell.SatisfyConditions(character) && !IsConquestVillageWithoutTerritory())
                        {
                            entity.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_CONDITIONS_UNSATISFIED));
                            return;
                        }

                        entity.CellId = cellId;
                        TryOpenDoorSwitch(character, cellId);
                        ApplyTriggerActions(character, cell, cellId);
                        return;
                    }                    
                }     

                entity.CellId = cellId;
                TryOpenDoorSwitch(character, cellId);
                return;
            }

            entity.CellId = cellId;
        }

        /// <summary>
        /// Nulls the raw string fields that are only needed during construction/cloning.
        /// Call this from MapManager after all Clone() calls for this map are complete.
        /// Do NOT call on maps that are still used as Clone() sources (m_creationIds).
        /// </summary>
        public void FreeRawData()
        {
            // Only the encoded cell string is safe to drop — DataKey and CreateTime
            // are sent on every GDM packet when a player enters the map.
            Data = null;
        }

        /// <summary>
        ///
        /// </summary>
        public new void Dispose()
        {
            SubArea.RemoveUpdatable(this);
            SubArea.RemoveHandler(base.Dispatch);

            m_entityById.Clear();
            m_entityById = null;

            m_entityByName.Clear();
            m_entityByName = null;

            m_moveableEntities.Clear();
            m_moveableEntities = null;

            m_monsterGroups.Clear();
            m_monsterGroups = null;

            m_taxCollector = null;

            m_occupiedCells.Clear();
            m_occupiedCells = null;

            m_cellsArray = null;
            m_walkableCellIds = null;

            m_animatedDoorByCellId.Clear();
            m_animatedDoorByCellId = null;

            m_doorCellEncodings = null;

            m_subArea = null;

            Pathmaker = null;

            base.Dispose();
        }
    }    
}
