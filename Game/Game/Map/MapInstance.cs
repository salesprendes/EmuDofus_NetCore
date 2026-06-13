using Game.Action;
using Game.Area;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity;
using Game.House;
using Game.Interactive;
using Game.Interactive.Type;
using Game.Job;
using Game.Manager;
using Game.Mount;
using Game.Network;
using Game.Spawn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Game.Map
{
    public sealed class MapInstance : MessageDispatcher, IMovementHandler, IDisposable
    {
        private static string HASH_CELL = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

        private const int DOOR_OPEN_MOVEMENT = 4;
        private const string CELL_MOVEMENT_MASK = "801";
        private const int MAP_SYNC_MOVEMENT_GRACE = 10000;


        private static readonly int[] s_hashCellIndex = BuildHashCellIndex();
        private static int[] BuildHashCellIndex()
        {
            var idx = new int[128];
            for (int i = 0; i < idx.Length; i++)
            {
                idx[i] = -1;
            }

            for (int i = 0; i < HASH_CELL.Length; i++)
            {
                idx[HASH_CELL[i]] = i;
            }

            return idx;
        }


        private static long m_NextMonsterId;

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

        private static readonly Dictionary<int, DoorAnimationDefinition[]> s_doorAnimationsByMap = new Dictionary<int, DoorAnimationDefinition[]>
        {
            { 736,   new[] { new DoorAnimationDefinition(224, 4700, 50000, 3700) } },
            { 8538,  new[] { new DoorAnimationDefinition(125, 4700, 25000, 3700) } },
            { 10352, new[] { new DoorAnimationDefinition(98,  3333, 30000, 3700) } },

            { 8214,  new[] { new DoorAnimationDefinition(403, 500, -1, 500), new DoorAnimationDefinition(373, 500, -1, 500) } },
            { 7951,  new[] { new DoorAnimationDefinition(323, 500, -1, 500), new DoorAnimationDefinition(295, 500, -1, 500) } },
            { 7896,  new[] { new DoorAnimationDefinition(284, 500, -1, 500), new DoorAnimationDefinition(325, 500, -1, 500) } },
            { 8268,  new[] { new DoorAnimationDefinition(307, 500, -1, 500), new DoorAnimationDefinition(353, 500, -1, 500) } },

            { 8346,  new[] { new DoorAnimationDefinition(141, 500, -1, 500) } },
            { 8076,  new[] { new DoorAnimationDefinition(106, 500, -1, 500), new DoorAnimationDefinition(108, 500, -1, 500) } },
            { 8137,  new[] { new DoorAnimationDefinition(270, 500, -1, 500) } },

        };

        private static readonly Dictionary<int, DoorSwitchDefinition[]> s_doorSwitchesByMap = new Dictionary<int, DoorSwitchDefinition[]>
        {
            { 736, new[] { new DoorSwitchDefinition(224, new[] { 260 }, 1, 50000) } },
            { 8538, new[] { new DoorSwitchDefinition(125, new[] { 88 }, 1, 25000) } },
            { 10352, new[] { new DoorSwitchDefinition(98, new[] { 299, 327, 355 }, 1, 30000) } },
        };

        private static long NextMonsterId => Interlocked.Decrement(ref m_NextMonsterId);

        public FieldTypeEnum FieldType => FieldTypeEnum.TYPE_MAP;

        public Pathmaker Pathmaker
        {
            get;
            private set;
        }

        public FightManager FightManager
        {
            get;
            private set;
        }

        public int Id
        {
            get;
            private set;
        }

        public int SubAreaId
        {
            get;
            private set;
        }

        public int X
        {
            get;
            private set;
        }

        public int Y
        {
            get;
            private set;
        }

        public int Width
        {
            get;
            private set;
        }

        public int Height
        {
            get;
            private set;
        }

        public string Data
        {
            get;
            private set;
        }

        public string DataKey
        {
            get;
            private set;
        }

        public string CreateTime
        {
            get;
            private set;
        }

        public List<int> FightTeam0Cells
        {
            get;
            private set;
        }

        public List<int> FightTeam1Cells
        {
            get;
            private set;
        }

        public SubAreaInstance SubArea
        {
            get
            {
                if (m_subArea == null)
                {
                    m_subArea = AreaManager.Instance.GetSubArea(SubAreaId);
                }

                return m_subArea;
            }
        }

        public IEnumerable<AbstractEntity> Entities => m_entityById.Values;

        public TaxCollectorEntity TaxCollector => m_taxCollector;

        public IReadOnlyList<MapCell> Cells => m_cellsArray;

        public bool CanAbortMovement => true;

        public int RandomTeleportCell
        {
            get
            {
                var actionCell = Array.Find(m_cellsArray, cell => cell.Trigger != null);

                if (actionCell != null)
                {
                    return actionCell.Id;
                }

                actionCell = Array.Find(m_cellsArray, cell => cell.Walkable);
                if (actionCell != null)
                {
                    return actionCell.Id;
                }

                return -1;
            }
        }

        public int RandomFreeCell(int excludedCell = -1, HashSet<int> rejectedCells = null)
        {
            HashSet<int> fightingCharCells = FightManager.Fights.SelectMany(f => f.Fighters).Where(f => f.Type == EntityTypeEnum.TYPE_CHARACTER).Select(f => f.CellId).ToHashSet();

            bool Allowed(int id) => id != excludedCell && (rejectedCells == null || !rejectedCells.Contains(id));

            var candidates = m_walkableCellIds.Where(id => Allowed(id) && m_cellsArray[id].Walkable && !m_cellsArray[id].IsDestinationOnly && !m_occupiedCells.Contains(id) && !fightingCharCells.Contains(id)).ToList();

            if (candidates.Count > 0)
                return candidates[Util.Next(0, candidates.Count)];

            candidates = m_walkableCellIds.Where(id => Allowed(id) && m_cellsArray[id].Walkable && !m_occupiedCells.Contains(id) && !fightingCharCells.Contains(id)).ToList();

            if (candidates.Count > 0)
                return candidates[Util.Next(0, candidates.Count)];

            candidates = m_walkableCellIds.Where(id => Allowed(id) && !fightingCharCells.Contains(id)).ToList();

            if (candidates.Count > 0)
                return candidates[Util.Next(0, candidates.Count)];

            candidates = m_walkableCellIds.Where(id => Allowed(id)).ToList();

            return candidates.Count > 0 ? candidates[Util.Next(0, candidates.Count)] : -1;
        }

        private void SetEntityCell(AbstractEntity entity, int cellId)
        {
            if (entity.CellId == cellId)
            {
                return;
            }

            if (m_entityById.ContainsKey(entity.Id))
            {
                m_occupiedCells.Remove(entity.CellId);
                if (cellId >= 0)
                {
                    m_occupiedCells.Add(cellId);
                }
            }

            entity.CellId = cellId;
        }

        public int PlayerCount => m_playerCount;

        public bool IsInitialized => m_initialized;

        public IEnumerable<InteractiveObject> InteractiveObjects => m_interactiveObjects;

        public Paddock Paddock => m_paddock;

        public HouseInstance House => m_house;

        public HouseInstance GetHouseByOutsideCellId(int cellId)
        {
            if (m_housesOutside == null) return null;
            foreach (var house in m_housesOutside)
                if (house.CellIdOutside == cellId) return house;
            return null;
        }

        private Dictionary<long, AbstractEntity> m_entityById;
        private Dictionary<string, AbstractEntity> m_entityByName;
        private Dictionary<int, AnimatedDoor> m_animatedDoorByCellId;
        private Dictionary<int, (string closed, string open)> m_doorCellEncodings;
        private MapCell[] m_cellsArray;
        private int[] m_walkableCellIds;
        private List<InteractiveObject> m_interactiveObjects;
        private SubAreaInstance m_subArea;
        private Paddock m_paddock;
        private HouseInstance m_house;
        private List<HouseInstance> m_housesOutside;
        private bool m_subInstance;
        private int m_playerCount;
        private bool m_initialized;
        private bool m_interactiveObjectsRegistered;
        private SpawnQueue m_spawnQueue;
        private List<MonsterSpawnDAO> m_monsters;
        private List<AbstractEntity> m_moveableEntities;
        private List<MonsterGroupEntity> m_monsterGroups;
        private TaxCollectorEntity m_taxCollector;
        private ConquestPrismEntity m_conquestPrism;
        private HashSet<int> m_occupiedCells;

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
            m_house = HouseManager.Instance.GetByInsideMapId(Id);
            m_housesOutside = HouseManager.Instance.GetAllByOutsideMapId(Id);

            FightManager = new FightManager(this);
            SubArea.AddUpdatable(this);

            if (!m_subInstance)
            {

                SubArea.SafeAddHandler(base.Dispatch);
                SpawnManager.Instance.RegisterMap(this);
            }


            Initialize();
        }

        private void Initialize()
        {
            var triggers = MapTriggerRepository.Instance.GetTriggers(Id);


            Dictionary<int, MapTriggerDAO> triggerByCellId = null;
            if (triggers.Count > 0)
            {
                triggerByCellId = new Dictionary<int, MapTriggerDAO>(triggers.Count);
                foreach (var t in triggers)
                {
                    if (!triggerByCellId.ContainsKey(t.CellId))
                    {
                        triggerByCellId.Add(t.CellId, t);
                    }
                }
            }

            int cellCount = Data.Length / 10;
            m_cellsArray = new MapCell[cellCount];
            var walkableIds = new List<int>();




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
                    {
                        m_animatedDoorByCellId[id] = door;
                    }
                }
                m_cellsArray[id] = cell;
                if (cell.Walkable)
                {
                    walkableIds.Add(id);
                }
            }

            m_walkableCellIds = walkableIds.ToArray();

            InitializeDoorAnimations();



            if (m_animatedDoorByCellId.Count > 0)
            {
                m_doorCellEncodings = new Dictionary<int, (string closed, string open)>(m_animatedDoorByCellId.Count);
                foreach (var cellId in m_animatedDoorByCellId.Keys)
                {
                    if (cellId < cellCount)
                    {
                        m_doorCellEncodings[cellId] = (closed: EncodeCellBytes(rawBytes, cellId * 10), open: EncodeCellBytesWithMovement(rawBytes, cellId * 10, DOOR_OPEN_MOVEMENT));
                    }
                }
            }


            Pathmaker = new Pathmaker(this);
        }

        private static string EncodeCellBytes(byte[] raw, int offset)
        {
            var chars = new char[10];
            for (int i = 0; i < 10; i++)
            {
                chars[i] = HASH_CELL[raw[offset + i]];
            }

            return new string (chars);
        }

        private static string EncodeCellBytesWithMovement(byte[] raw, int offset, int movement)
        {
            var chars = new char[10];
            for (int i = 0; i < 10; i++)
            {
                chars[i] = HASH_CELL[i == 2 ? (byte)((raw[offset + 2] & ~56) | ((movement & 7) << 3)) : raw[offset + i]];
            }

            return new string (chars);
        }

        private void InitializeDoorAnimations()
        {
            DoorAnimationDefinition[] definitions;
            if (!s_doorAnimationsByMap.TryGetValue(Id, out definitions))
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition.CellId >= m_cellsArray.Length || m_animatedDoorByCellId.ContainsKey(definition.CellId))
                {
                    continue;
                }

                var door = definition.Create(this);
                m_animatedDoorByCellId.Add(definition.CellId, door);
                m_interactiveObjects.Add(door);
            }
        }

        public MapInstance Clone()
        {
            return new MapInstance(SubAreaId, Id, X, Y, Width, Height, Data, DataKey, CreateTime, FightTeam0Cells, FightTeam1Cells, true);
        }

        public void SetSpawnQueue(SpawnQueue spawnQueue)
        {
            m_spawnQueue = spawnQueue;
        }

        private void RegisterAllInteractiveObjects()
        {
            if (m_interactiveObjectsRegistered)
            {
                return;
            }

            m_interactiveObjectsRegistered = true;
            foreach (var obj in m_interactiveObjects)
            {
                AddUpdatable(obj);
                obj.AddHandler(Dispatch);
            }
        }

        private void InitializeOnFirstPlayerEnter()
        {
            if (m_initialized)
            {
                return;
            }

            m_initialized = true;
            RegisterAllInteractiveObjects();
            UpdateConquestDoors();
            InitPrismSpawn();
            InitNpcsSpawn();
            InitMonstersSpawn();
            InitEntitiesMovements();
        }

        private void InitNpcsSpawn()
        {
            foreach (var npc in NpcManager.Instance.GetByMapId(Id))
            {
                SpawnEntity(new NonPlayerCharacterEntity(npc, npc.Id));
            }
        }

        private bool IsConquestVillageWithoutTerritory()
        {
            if (!SubArea.CanConquest)
            {
                return false;
            }

            var area = SubArea.Area;
            return area != null && ConquestManager.IsVillageArea(area.Id) && !ConquestManager.Instance.IsVillageAreaConquered(area.Id);
        }



        private void UpdateConquestDoors()
        {
            if (m_animatedDoorByCellId.Count == 0)
            {
                return;
            }

            var area = SubArea.Area;
            if (area == null || !ConquestManager.IsVillageArea(area.Id))
            {
                return;
            }

            bool neutral = !ConquestManager.Instance.IsVillageAreaConquered(area.Id);
            foreach (var door in m_animatedDoorByCellId.Values)
            {
                if (neutral)
                {
                    door.ForceOpen();
                }
                else
                {
                    door.ForceClose();
                }
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
            {
                return;
            }

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
            {
                SpawnConquestPrism(prism);
            }
        }

        public void ScheduleConquestDoorUpdate()
        {
            AddMessage(() => { UpdateConquestDoors(); if (m_initialized) { UpdateConquestPrism(); } });
        }

        private void InitMonstersSpawn()
        {
            m_monsters = new List<MonsterSpawnDAO>(MonsterSpawnRepository.Instance.GetById(ZoneTypeEnum.TYPE_MAP, Id).OrderByDescending(spawn => spawn.Probability));

            if (IsConquestVillageWithoutTerritory() || m_monsters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < WorldConfig.SPAWN_MAX_GROUP_PER_MAP; i++)
            {
                SpawnMonsters();
            }
        }

        private void InitEntitiesMovements()
        {
            AddTimer(5000, ProcessEntitiesMovements);
        }

        private void ProcessEntitiesMovements()
        {
            if (m_playerCount == 0)
            {
                return;
            }

            for (int i = 0; i < m_moveableEntities.Count; i++)
            {
                MoveEntity(m_moveableEntities[i]);
            }
        }

        private void DelayEntityMovements(bool stopCurrentMovements)
        {
            int count = m_moveableEntities.Count;
            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var entity = m_moveableEntities[i];
                if (stopCurrentMovements && entity.CurrentAction != null && entity.CurrentAction.Type == GameActionTypeEnum.MAP_MOVEMENT)
                {
                    entity.StopAction(GameActionTypeEnum.MAP_MOVEMENT);
                }

                if (entity.MovementInterval == 0)
                {
                    entity.MovementInterval = Util.Next(10000, 25000);
                }

                var nextMovementTime = UpdateTime + MAP_SYNC_MOVEMENT_GRACE + (long)entity.MovementInterval * i / count;
                if (!stopCurrentMovements || entity.NextMovementTime < nextMovementTime)
                {
                    entity.NextMovementTime = nextMovementTime;
                }
            }
        }

        public void MoveEntity(AbstractEntity entity)
        {
            if (entity.MovementInterval == 0)
            {
                entity.MovementInterval = Util.Next(10000, 25000);
            }

            if (entity.NextMovementTime == 0)
            {
                entity.NextMovementTime = UpdateTime + entity.MovementInterval;
            }

            if (entity.NextMovementTime > UpdateTime)
            {
                return;
            }

            entity.NextMovementTime = UpdateTime + entity.MovementInterval;

            if (m_playerCount == 0)
            {
                return;
            }

            var cellId = entity.LastCellId;
            if (cellId < 1)
            {
                cellId = GetNearestMovementCell(entity.CellId);
            }

            if (entity.LastCellId == 0)
            {
                entity.LastCellId = entity.CellId;
            }
            else
            {
                entity.LastCellId = 0;
            }

            if (cellId < 1)
            {
                return;
            }

            entity.StopAction(GameActionTypeEnum.MAP_MOVEMENT);

            Move(entity, entity.CellId, Pathmaker.FindPathAsString(entity.CellId, cellId, false));
            AddMessage(() => entity.StopAction(GameActionTypeEnum.MAP_MOVEMENT));
        }

        public MapCell GetCell(int id)
        {
            return id >= 0 && id < m_cellsArray.Length ? m_cellsArray[id] : null;
        }

        public int GetNearestCell(int cellId)
        {
            foreach (var nextCell in CellZone.GetAdjacentCells(this, cellId))
            {
                var cell = GetCell(nextCell);
                if (cell != null && cell.Walkable)
                {
                    return nextCell;
                }
            }
            return -1;
        }

        public int GetNearestMovementCell(int cellId)
        {
            var rand = Util.Next(0, 101);
            var direction = DirectionEnum.Este;
            if (rand < 25)
            {
                direction = DirectionEnum.Sur;
            }
            else if (rand < 50)
            {
                direction = DirectionEnum.Oeste;
            }
            else if (rand < 75)
            {
                direction = DirectionEnum.Norte;
            }

            var nextCellId = Pathfinding.NextCell(this, cellId, direction);
            var cell = GetCell(nextCellId);
            if (cell != null && cell.Walkable)
            {
                if (cell.Walkable)
                {
                    return nextCellId;
                }
            }

            return -1;
        }

        public bool IsWalkable(int cellId)
        {
            MapCell cell = GetCell(cellId);
            if (cell != null)
            {
                return cell.Walkable || IsAnimatedDoorOpen(cellId);
            }

            return false;
        }

        public bool IsAnimatedDoorOpen(int cellId)
        {
            AnimatedDoor door;
            return m_animatedDoorByCellId.TryGetValue(cellId, out door) && door.IsOpened;
        }

        public void SendAnimatedDoorCellState(int cellId, bool opened)
        {
            var message = BuildDoorCellStateMessage(cellId, opened);
            if (message != null)
            {
                Dispatch(message);
            }
        }

        private string BuildDoorCellStateMessage(int cellId, bool opened)
        {
            if (m_doorCellEncodings == null || !m_doorCellEncodings.TryGetValue(cellId, out var encodings))
            {
                return null;
            }

            string data = opened ? encodings.open : encodings.closed;
            return WorldMessage.GAME_DATA_CELL(cellId, data, CELL_MOVEMENT_MASK);
        }

        public AbstractEntity GetEntity(long id)
        {
            if (m_entityById.ContainsKey(id))
            {
                return m_entityById[id];
            }

            return null;
        }

        public void ScheduleRepop(int excludedCell)
        {
            var delay = Util.Next(WorldConfig.MONSTER_REPOP_DELAY_MIN, WorldConfig.MONSTER_REPOP_DELAY_MAX);

            AddTimer(delay, () =>
            {
                if (m_monsters.Count == 0 || FightTeam1Cells.Count == 0)
                    return;

                if (m_monsterGroups.Count >= WorldConfig.SPAWN_MAX_GROUP_PER_MAP)
                    return;

                Logger.Debug($"[MapInstance] Mapa {Id}: creando nuevo grupo.");

                SpawnMonsters(excludedCell);
            }, oneshot: true);
        }

        public void SpawnMonsters(int excludedCell = -1) => SpawnMonsterGroup(m_monsters, excludedCell);
        public void SpawnMonsters(IEnumerable<MonsterSpawnDAO> monsters) => SpawnMonsterGroup(monsters, -1);


        private void SpawnMonsterGroup(IEnumerable<MonsterSpawnDAO> pool, int excludedCell)
        {
            if (IsConquestVillageWithoutTerritory() || FightTeam1Cells.Count == 0)
            {
                return;
            }

            var spawns = pool as IReadOnlyList<MonsterSpawnDAO> ?? pool?.ToList();
            if (spawns == null || spawns.Count == 0)
            {
                return;
            }

            var maxAggressionRange = spawns.Max(spawn => spawn.Grade.Template.AggressionRange);


            for (int pass = 0; pass < 2; pass++)
            {
                var avoidActiveFightAggro = pass == 0;
                var rejectedCells = new HashSet<int>();

                for (int attempts = 0; attempts < m_walkableCellIds.Length; attempts++)
                {
                    var cellId = RandomFreeCell(excludedCell, rejectedCells);
                    if (cellId < 0)
                    {
                        break;
                    }

                    if (avoidActiveFightAggro && maxAggressionRange > 0 && CellAggroesActiveFight(cellId, maxAggressionRange))
                    {
                        rejectedCells.Add(cellId);
                        continue;
                    }

                    var group = new MonsterGroupEntity(NextMonsterId, Id, cellId, spawns, FightTeam1Cells.Count);
                    if (group.HasMonsters)
                    {
                        SpawnEntity(group);
                    }

                    return;
                }
            }
        }

        private bool CellAggroesActiveFight(int cellId, int aggressionRange)
        {
            foreach (var fight in FightManager.Fights)
            {
                foreach (var fighter in fight.Fighters)
                {
                    if (fighter.Type == EntityTypeEnum.TYPE_CHARACTER && Pathfinding.GoalDistance(this, fighter.CellId, cellId) <= aggressionRange)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool StartMonsterFight(CharacterEntity character, IEnumerable<MonsterGradeDAO> grades)
        {
            var cellId = RandomFreeCell();
            if (cellId < 0)
            {
                return false;
            }

            var group = new MonsterGroupEntity(NextMonsterId, Id, cellId, grades);
            return group.HasMonsters && FightManager.StartMonsterFight(character, group);
        }

        public void SpawnEntity(AbstractEntity entity)
        {
            AddMessage(() =>
            {
                if (!m_entityById.ContainsKey(entity.Id))
                {
                    m_entityById.Add(entity.Id, entity);
                    m_occupiedCells.Add(entity.CellId);


                    if (entity.CanBeMoved())
                    {
                        m_moveableEntities.Add(entity);
                        if (entity.MovementInterval == 0)
                        {
                            entity.MovementInterval = Util.Next(10000, 25000);
                        }

                        if (entity.NextMovementTime == 0)
                        {
                            entity.NextMovementTime = UpdateTime + entity.MovementInterval;
                        }
                    }

                    if (entity is MonsterGroupEntity mg)
                    {
                        m_monsterGroups.Add(mg);
                    }

                    if (entity is TaxCollectorEntity tc)
                    {
                        m_taxCollector = tc;
                    }

                    if (entity is ConquestPrismEntity cp)
                    {
                        m_conquestPrism = cp;
                    }

                    if (m_subInstance)
                    {
                        entity.SetMap(this);
                    }

                    Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_ADD, entity));
                    AddUpdatable(entity);

                    if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                    {
                        InitializeOnFirstPlayerEnter();

                        m_playerCount++;
                        m_entityByName.Add(entity.Name.ToLower(), entity);


                        if (m_playerCount == 1)
                        {
                            DelayEntityMovements(false);
                        }

                        AddHandler(entity.Dispatch);
                        SendAllInformations(entity);
                    }
                }
                else
                {
                    Logger.Error($"MapInstance::SpawnEntity: ya existe una entidad con el mismo id: {entity.Name}");

                    WorldService.Instance.AddUpdatable(entity);
                }
            });
        }

        public void SendAllInformations(AbstractEntity entity)
        {
            entity.CachedBuffer = true;

            DelayEntityMovements(true);


            SendMapInformations(entity);
            SendInteractiveData(entity);
            SendPaddockInformations(entity);
            SendHouseInformations(entity);
            entity.Dispatch(WorldMessage.GAME_DATA_SUCCESS());
            SendAnimatedDoorRuntimeStates(entity);


            SendFightCount(entity);
            SendFightsInformations(entity);

            entity.CachedBuffer = false;
        }

        public void SendFightsInformations(AbstractEntity entity)
        {
            foreach (var fight in FightManager.Fights)
            {
                fight.SendMapFightInfos(entity);
            }
        }

        public void SendFightCount(AbstractEntity entity)
    => entity.Dispatch(WorldMessage.FIGHT_COUNT(FightManager.FightCount));

        public void SendMapInformations(AbstractEntity entity)
    => entity.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_ADD, Entities.ToArray()));

        public void SendInteractiveData(AbstractEntity entity)
        {
            entity.Dispatch(WorldMessage.INTERACTIVE_DATA_FRAME(m_interactiveObjects));
        }

        private void SendAnimatedDoorRuntimeStates(AbstractEntity entity)
        {
            foreach (var door in m_animatedDoorByCellId.Values)
            {
                if (door.IsClosed)
                {
                    continue;
                }

                if (door.IsOpened)
                {
                    var message = BuildDoorCellStateMessage(door.CellId, true);
                    if (message != null)
                    {
                        entity.Dispatch(message);
                    }
                }

                door.SendUpdateTo(entity);
            }
        }

        public void SendPaddockInformations(AbstractEntity entity)
        {
            if (m_paddock != null)
            {
                m_paddock.SendInformations(entity);
            }
        }

        public void SendHouseInformations(AbstractEntity entity)
        {
            if (!(entity is CharacterEntity character))
                return;

            if (m_house != null)
                m_house.SendInformationsTo(character);

            if (m_housesOutside != null)
                foreach (var house in m_housesOutside)
                    house.SendInformationsTo(character);
        }

        public void DestroyEntity(AbstractEntity entity)
        {
            if (m_entityById.ContainsKey(entity.Id))
            {
                m_entityById.Remove(entity.Id);
                m_occupiedCells.Remove(entity.CellId);


                if (entity.CanBeMoved())
                {
                    m_moveableEntities.Remove(entity);
                }

                if (entity is MonsterGroupEntity mg)
                {
                    m_monsterGroups.Remove(mg);
                }

                if (entity is TaxCollectorEntity)
                {
                    m_taxCollector = null;
                }

                if (entity is ConquestPrismEntity)
                {
                    m_conquestPrism = null;
                }

                RemoveUpdatable(entity);
                Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_REMOVE, entity));

                if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                {
                    RemoveHandler(entity.Dispatch);

                    m_entityByName.Remove(entity.Name.ToLower());
                    m_playerCount--;


                    if (m_playerCount == 0 && m_subInstance)
                    {
                        MapManager.Instance.ReleaseInstance(this);
                    }
                }
            }
        }

        public void InteractiveExecute(CharacterEntity character, int cellId, int skillId)
        {
            var cell = GetCell(cellId);
            if (cell != null)
            {
                if (cell.InteractiveObject != null)
                {
                    var skill = character.CharacterJobs.GetSkill(skillId);

                    if (skill == null && !cell.InteractiveObject.CanUseWithoutJobSkill(skillId))
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

        public bool IsInInteractiveSkillRange(CharacterEntity character, int sourceCellId, int targetCellId, int skillId)
        {
            if (character == null || character.MapId != Id || !Pathfinding.IsValidCellId(this, sourceCellId) || GetCell(targetCellId) == null)
            {
                return false;
            }

            if (Pathfinding.GoalDistance(this, sourceCellId, targetCellId) <= 1)
            {
                return true;
            }

            return IsFishingSkillInRange(character, sourceCellId, targetCellId, skillId);
        }

        private bool IsFishingSkillInRange(CharacterEntity character, int sourceCellId, int targetCellId, int skillId)
        {
            var skill = character.CharacterJobs.GetSkill(skillId);
            if (skill == null)
            {
                return false;
            }

            var jobId = character.CharacterJobs.GetJobId(skill.Id);
            if (jobId != (int)JobIdEnum.JOB_PECHEUR)
            {
                return false;
            }

            var weapon = character.Inventory?.Items.Find(item => item.Slot == ItemSlotEnum.SLOT_WEAPON);
            var range = weapon == null ? 0 : GetFishingRodRange(weapon.TemplateId);

            return range > 0 && Pathfinding.GoalDistance(this, sourceCellId, targetCellId) <= range;
        }

        private static int GetFishingRodRange(int templateId)
        {
            switch (templateId)
            {
                case 8541:
                case 6661:
                case 596:
                    return 2;
                case 1866:
                    return 3;
                case 1865:
                case 1864:
                    return 4;
                case 1867:
                case 2188:
                    return 5;
                case 1863:
                case 1862:
                    return 6;
                case 1868:
                    return 7;
                case 1861:
                case 1860:
                    return 8;
                case 2366:
                    return 9;
                default:
                    return 0;
            }
        }

        public MovementPath DecodeMovement(AbstractEntity entity, int cellId, string path)
        {
            return Pathfinding.IsValidPath(entity, this, cellId, path);
        }

        public void Move(AbstractEntity entity, int cellId, string movementPath)
        {
            AddMessage(() =>
                {
                    var decoded = Pathfinding.DecodePath(this, cellId, movementPath);
                    var destCellId = decoded.TransitCells.Count > 0 ? decoded.EndCell : -1;
                    var destCell = GetCell(destCellId);
                    var path = DecodeMovement(entity, cellId, movementPath);

                    if (path != null && path.MovementLength > 0)
                    {
                        entity.Move(path);

                        if (entity is CharacterEntity charRtt)
                            charRtt.Dispatch(WorldMessage.BASIC_RPING(System.Environment.TickCount64));

                        if (entity is CharacterEntity character && character.CurrentAction is GameMapMovementAction action)
                        {
                            var skillAttached = false;
                            if (character.TryGetPendingInteractiveSkill(Id, out var pendingCellId, out var pendingSkillId))
                            {
                                if (pendingCellId == destCellId || IsInInteractiveSkillRange(character, path.EndCell, pendingCellId, pendingSkillId))
                                {
                                    action.SkillCellId = pendingCellId;
                                    action.SkillId = pendingSkillId;
                                    action.SkillMapId = Id;
                                    skillAttached = true;
                                }

                                character.ClearPendingInteractiveSkill();
                            }

                            if (!skillAttached)
                            {
                                var implicitSkillId = destCell?.InteractiveObject?.GetImplicitSkillId(character) ?? -1;
                                if (implicitSkillId != -1)
                                {
                                    action.SkillCellId = destCellId;
                                    action.SkillId = implicitSkillId;
                                    action.SkillMapId = Id;
                                }
                            }
                        }
                    }
                    else if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                    {
                        CharacterEntity character = (CharacterEntity)entity;

                        if (TryStartAggroFight(character, character.CellId))
                        {
                            return;
                        }

                        int implicitSkillId = destCell?.InteractiveObject?.GetImplicitSkillId(character) ?? -1;

                        if (implicitSkillId != -1)
                        {
                            character.Dispatch(WorldMessage.GAME_ACTION(0, character.Id));
                            InteractiveExecute(character, destCellId, implicitSkillId);
                        }
                        else if (destCell?.InteractiveObject != null && destCell.InteractiveObject.IsActive)
                        {
                            character.Dispatch(WorldMessage.GAME_ACTION(0, character.Id));
                        }
                        else
                        {
                            character.Dispatch(WorldMessage.GAME_ACTION(0, character.Id));
                        }

                        character.ClearPendingInteractiveSkill();
                    }

                });
        }

        public bool CanBeAggro(CharacterEntity character, int cellId, MonsterGroupEntity monsters)
        {
            if (character == null || monsters == null || character.IsGhost || character.IsTombestone)
            {
                return false;
            }

            return Pathfinding.GoalDistance(this, cellId, monsters.CellId) <= monsters.AggressionRange
                && ((character.AlignmentId == (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL && monsters.AlignmentId == -1) || (character.AlignmentId != (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL && monsters.AlignmentId != character.AlignmentId));
        }

        public bool HasAggroNear(CharacterEntity character, int cellId)
        {
            for (int i = 0; i < m_monsterGroups.Count; i++)
            {
                var mg = m_monsterGroups[i];


                if (mg.AggressionRange > 0 && CanBeAggro(character, cellId, mg))
                    return true;
            }
            return false;
        }

        private bool TryStartAggroFight(CharacterEntity character, int cellId)
        {
            if (character == null || !character.CanGameAction(GameActionTypeEnum.FIGHT))
            {
                return false;
            }

            foreach (var monsterGroup in m_monsterGroups.ToArray())
            {
                if (!CanBeAggro(character, cellId, monsterGroup))
                {
                    continue;
                }

                SetEntityCell(character, cellId);
                if (monsterGroup.AlignmentId == -1)
                {
                    if (FightManager.StartMonsterFight(character, monsterGroup))
                    {
                        return true;
                    }
                }
                else if (FightManager.StartAggression(monsterGroup, character))
                {
                    return true;
                }
            }

            return false;
        }

        private int OpenDoorForPassage(int cellId)
        {
            AnimatedDoor door;
            if (!m_animatedDoorByCellId.TryGetValue(cellId, out door))
            {
                return 0;
            }

            return door.OpenTemporarily();
        }

        private void ApplyTriggerActions(CharacterEntity character, MapCell cell, int cellId)
        {
            var delay = OpenDoorForPassage(cellId);
            if (delay <= 0)
            {
                cell.ApplyActions(character);
                return;
            }

            AddTimer(delay, () => { if (character.MapId == Id && character.CellId == cellId) { cell.ApplyActions(character); } }, true);
        }

        private void TryOpenDoorSwitch(CharacterEntity character, int cellId)
        {
            DoorSwitchDefinition[] definitions;
            if (!s_doorSwitchesByMap.TryGetValue(Id, out definitions))
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (!definition.HasTriggerCell(cellId) || CountOccupiedSwitchCells(definition.TriggerCellIds) < definition.RequiredPlayers)
                {
                    continue;
                }

                AnimatedDoor door;
                if (m_animatedDoorByCellId.TryGetValue(definition.DoorCellId, out door))
                {
                    door.OpenTemporarily(definition.OpenedDuration);
                }
            }
        }

        private int CountOccupiedSwitchCells(int[] cellIds)
        {
            var count = 0;
            foreach (var cellId in cellIds)
            {
                if (m_entityById.Values.OfType<CharacterEntity>().Any(character => character.CellId == cellId))
                {
                    count++;
                }
            }
            return count;
        }

        public void MovementFinish(AbstractEntity entity, MovementPath path, int cellId)
        {
            if (entity.CellId == cellId)
            {
                if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
                {
                    TryStartAggroFight((CharacterEntity)entity, cellId);
                }

                return;
            }

            if (entity.Type == EntityTypeEnum.TYPE_CHARACTER)
            {
                var character = (CharacterEntity)entity;

                if (TryStartAggroFight(character, cellId))
                {
                    return;
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
                        if (!cell.SatisfyConditions(character) && !IsConquestVillageWithoutTerritory())
                        {
                            entity.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_CONDITIONS_UNSATISFIED));
                            return;
                        }

                        SetEntityCell(entity, cellId);
                        TryOpenDoorSwitch(character, cellId);
                        ApplyTriggerActions(character, cell, cellId);
                        return;
                    }
                }

                SetEntityCell(entity, cellId);
                TryOpenDoorSwitch(character, cellId);
                return;
            }

            SetEntityCell(entity, cellId);
        }

        public void FreeRawData()
        {
            Data = null;
        }

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
