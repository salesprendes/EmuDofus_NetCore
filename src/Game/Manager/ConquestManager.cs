using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Area;
using Game.Conquest;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Map;

namespace Game.Manager
{
    public sealed class ConquestManager : Singleton<ConquestManager>
    {
        public enum AlignmentTypeEnum
        {
            ALIGNMENT_NEUTRAL = 0,
            ALIGNMENT_BONTARIEN = 1,
            ALIGNMENT_BRAKMARIEN = 2,
            ALIGNMENT_MERCENARY = 3,
        }

        private const int ALIGNMENT_NEUTRAL = 0;
        private const int ALIGNMENT_NONE = -1;
        private const int ALIGNMENT_BONTA = 1;
        private const int ALIGNMENT_BRAKMAR = 2;
        private const int MIN_PLACE_LEVEL = 10;

        private readonly Dictionary<int, ConquestTerritory> m_bySubArea;
        private readonly Dictionary<int, int> m_prismMapBySubArea;

        private static readonly ConquestVillageDefinition[] s_villages =
        {
            new ConquestVillageDefinition(areaId: 13, territorySubAreaId: 63,  prismSubAreaId: 63),
            new ConquestVillageDefinition(areaId: 14, territorySubAreaId: 81,  prismSubAreaId: 81),
            // Pandala: bontaTemplateId / brakmarTemplateId per city
            // 538=FuegoBonta 539=TierraBonta 540=AireBonta 541=AkwaBonta
            // 542=AireBrakmar 543=TierraBrakmar 544=AkwaBrakmar 545=FuegoBrakmar
            new ConquestVillageDefinition(areaId: 20, territorySubAreaId: 117, prismSubAreaId: 122, prismMapId: 8368, prismCellId: 268, bontaTemplateId: 541, brakmarTemplateId: 544), // Akwadala
            new ConquestVillageDefinition(areaId: 21, territorySubAreaId: 118, prismSubAreaId: 123, bontaTemplateId: 540, brakmarTemplateId: 542), // Aerdala
            new ConquestVillageDefinition(areaId: 22, territorySubAreaId: 116, prismSubAreaId: 121, bontaTemplateId: 538, brakmarTemplateId: 545), // Feudala (Fuegodala)
            new ConquestVillageDefinition(areaId: 23, territorySubAreaId: 115, prismSubAreaId: 120, bontaTemplateId: 539, brakmarTemplateId: 543), // Terrdala (Tierralada)
        };

        private static readonly int[] s_villageAreaIds = s_villages.Select(village => village.AreaId).Distinct().ToArray();
        private static readonly Dictionary<int, ConquestVillageDefinition> s_villageByArea = s_villages.ToDictionary(village => village.AreaId);
        private static readonly Dictionary<int, ConquestVillageDefinition> s_villageByTerritorySubArea = s_villages.ToDictionary(village => village.TerritorySubAreaId);
        private static readonly Dictionary<int, ConquestVillageDefinition> s_villageByPrismSubArea = s_villages.ToDictionary(village => village.PrismSubAreaId);
        private static readonly Dictionary<int, ConquestVillageDefinition> s_villageByRoomMap = s_villages.Where(village => village.HasPrismRoom).ToDictionary(village => village.PrismMapId);

        private static readonly Dictionary<int, int[]> s_nearSubAreas = new Dictionary<int, int[]>
        {
            { 105, new[] { 106, 107, 108, 109, 119, 143, 171, 476 } },
            { 106, new[] { 105, 113, 117, 124, 171 } },
            { 107, new[] { 105, 112, 116, 171, 476 } },
            { 108, new[] { 105, 111, 115, 119, 171 } },
            { 109, new[] { 105, 114, 118, 119, 171 } },
            { 111, new[] { 108, 115 } },
            { 112, new[] { 107, 116 } },
            { 113, new[] { 106, 117 } },
            { 114, new[] { 109, 118 } },
            { 115, new[] { 108, 111, 120 } },
            { 116, new[] { 107, 112, 121 } },
            { 117, new[] { 106, 113, 122 } },
            { 118, new[] { 109, 114, 123 } },
            { 119, new[] { 105, 108, 109, 171 } },
            { 120, new[] { 115 } },
            { 121, new[] { 116 } },
            { 122, new[] { 117 } },
            { 123, new[] { 118 } },
            { 143, new[] { 105, 173 } },
            { 171, new[] { 105, 106, 107, 108, 109, 119 } },
            { 476, new[] { 105, 107 } },
        };

        public IEnumerable<ConquestTerritory> Territories => m_bySubArea.Values;

        public ConquestManager()
        {
            m_bySubArea = new Dictionary<int, ConquestTerritory>();
            m_prismMapBySubArea = new Dictionary<int, int>();
        }

        public void Initialize()
        {
            foreach (var record in ConquestTerritoryRepository.Instance.All)
            {
                ApplyVillageDefaults(record);
                m_bySubArea[record.SubAreaId] = new ConquestTerritory(record);
                if (record.PrismMapId > 0)
                    m_prismMapBySubArea[record.SubAreaId] = record.PrismMapId;
            }

            WorldService.Instance.AddTimer(WorldConfig.PRISM_HONOR_GAIN_INTERVAL, GainPrismHonor);

            Logger.Info("ConquestManager : " + m_bySubArea.Count + " territories loaded.");
        }

        private void GainPrismHonor()
        {
            foreach (var territory in m_bySubArea.Values)
            {
                if (territory.IsNeutral || territory.IsUnderAttack || !territory.IsPersisted)
                    continue;

                territory.SetPrismHonor(territory.PrismHonor + WorldConfig.PRISM_HONOR_GAIN_AMOUNT);
            }
        }

        public ConquestTerritory GetBySubArea(int subAreaId)
        {
            m_bySubArea.TryGetValue(ResolveTerritorySubAreaId(subAreaId), out var territory);
            return territory;
        }

        public ConquestTerritory GetByCharacterMap(CharacterEntity character)
        {
            if (character?.Map != null && s_villageByRoomMap.TryGetValue(character.Map.Id, out var village))
                return GetBySubArea(village.TerritorySubAreaId);

            var subArea = character?.Map?.SubArea;
            if (subArea == null)
                return null;

            var territory = GetBySubArea(subArea.Id);
            if (territory != null)
                return territory;

            return GetByArea(subArea.Area.Id);
        }

        public ConquestTerritory GetByArea(int areaId)
        {
            if (!IsVillageArea(areaId))
                return null;

            if (s_villageByArea.TryGetValue(areaId, out var village))
            {
                var villageTerritory = GetBySubArea(village.TerritorySubAreaId);
                if (villageTerritory != null)
                    return villageTerritory;
            }

            foreach (var territory in m_bySubArea.Values)
            {
                if (!AreaManager.Instance.TryGetSubArea(territory.SubAreaId, out var subArea))
                    continue;

                if (subArea.Area.Id == areaId)
                    return territory;
            }

            return null;
        }

        public bool IsVillageAreaConquered(int areaId)
        {
            var territory = GetByArea(areaId);
            return territory != null && territory.AlignmentId != ALIGNMENT_NEUTRAL;
        }

        public bool IsConquerableSubArea(SubAreaInstance subArea)
        {
            return subArea != null && subArea.CanConquest;
        }

        public bool CanPlacePrism(CharacterEntity character, int subAreaId)
        {
            subAreaId = ResolveTerritorySubAreaId(subAreaId);

            if (m_bySubArea.ContainsKey(subAreaId))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_ALREADY_CONQUERED));
                return false;
            }

            if (!AreaManager.Instance.TryGetSubArea(subAreaId, out var subArea))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_INVALID_MAP));
                return false;
            }

            if (!IsConquerableSubArea(subArea))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_INVALID_MAP));
                return false;
            }

            if (IsVillageArea(subArea.Area.Id) && IsVillageAreaConquered(subArea.Area.Id))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_ALREADY_CONQUERED));
                return false;
            }

            if (!HasAdjacentAlly(subAreaId, character.AlignmentId) && m_bySubArea.Values.Any(t => t.AlignmentId == character.AlignmentId))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PRISM_INVALID_MAP));
                return false;
            }

            return true;
        }

        public bool PlacePrism(CharacterEntity character, int subAreaId)
        {
            subAreaId = ResolveTerritorySubAreaId(subAreaId);
            if (!CanPlacePrism(character, subAreaId))
                return false;

            var prismLevel = ConquestPrismEntity.DefaultLevel;
            var prismLife = ConquestPrismEntity.GetMaxLifeForLevel(prismLevel);
            var record = new ConquestTerritoryDAO
            {
                SubAreaId = subAreaId,
                AlignmentId = character.AlignmentId,
                BonusType = 0,
                Life = prismLife,
                MaxLife = prismLife,
                State = 0,
                PrismMapId = character.MapId,
                PrismCellId = character.CellId,
                PrismLevel = prismLevel,
                PrismHonor = 0,
                PrismType = (int)GetPrismTypeForSubArea(subAreaId),
            };
            ApplyVillageDefaults(record);

            ConquestTerritoryRepository.Instance.Add(record);
            var territory = new ConquestTerritory(record);
            m_bySubArea[subAreaId] = territory;
            m_prismMapBySubArea[subAreaId] = character.MapId;

            DispatchAlignmentChanged(territory, false);

            if (AreaManager.Instance.TryGetSubArea(subAreaId, out var placedSubArea))
                foreach (var map in MapManager.Instance.GetByAreaId(placedSubArea.Area.Id))
                    map.ScheduleConquestDoorUpdate();

            character.Map?.ScheduleConquestDoorUpdate();

            return true;
        }

        public void RemoveTerritory(int subAreaId)
        {
            subAreaId = ResolveTerritorySubAreaId(subAreaId);

            if (!m_bySubArea.TryGetValue(subAreaId, out var territory))
                return;

            territory.Destroy();
            m_bySubArea.Remove(subAreaId);
            m_prismMapBySubArea.Remove(subAreaId);

            AreaManager.Instance.TryGetSubArea(subAreaId, out var subArea);
            var areaId = subArea?.Area?.Id ?? -1;
            var capturedSubAreaId = subAreaId;

            WorldService.Instance.AddMessage(() =>
            {
                WorldService.Instance.Dispatcher.Dispatch(WorldMessage.SUBAREA_ALIGNMENT_CHANGED(capturedSubAreaId, ALIGNMENT_NEUTRAL, false));
                if (areaId > 0)
                    WorldService.Instance.Dispatcher.Dispatch(WorldMessage.AREA_ALIGNMENT_CHANGED(areaId, ALIGNMENT_NEUTRAL));
            });

            if (subArea != null)
                foreach (var map in MapManager.Instance.GetByAreaId(subArea.Area.Id))
                    map.ScheduleConquestDoorUpdate();

            BroadcastConquestWorldData();
        }

        public void TerritoryCaptured(ConquestTerritory territory, Game.Map.MapInstance map)
        {
            if (territory == null)
                return;

            if (map != null)
            {
                m_prismMapBySubArea[territory.SubAreaId] = map.Id;
                if (territory.PrismMapId <= 0)
                    territory.SetPrismPosition(map.Id, territory.PrismCellId >= 0 ? territory.PrismCellId : map.RandomFreeCell);
                var mapRef = map;
                WorldService.Instance.AddMessage(() =>
                    WorldService.Instance.Dispatcher.Dispatch(WorldMessage.CONQUEST_PRISM_DEAD(mapRef)));
            }

            DispatchAlignmentChanged(territory, false);

            // Doors/prism must update on capture (neutral→conquered closes the building entrance)
            if (AreaManager.Instance.TryGetSubArea(territory.SubAreaId, out var capturedSubArea))
                foreach (var areaMap in MapManager.Instance.GetByAreaId(capturedSubArea.Area.Id))
                    areaMap.ScheduleConquestDoorUpdate();
        }

        // Returns the existing territory for subAreaId, or creates a temporary unpersisted one
        // for neutral-city fights. Returns null if the subArea is not conquerable.
        public ConquestTerritory GetOrCreateNeutralForAttack(int subAreaId)
        {
            subAreaId = ResolveTerritorySubAreaId(subAreaId);
            if (m_bySubArea.TryGetValue(subAreaId, out var existing))
                return existing;

            if (!AreaManager.Instance.TryGetSubArea(subAreaId, out var subArea))
                return null;
            if (!IsConquerableSubArea(subArea))
                return null;

            int prismLevel = ConquestPrismEntity.DefaultLevel;
            int prismLife = ConquestPrismEntity.GetMaxLifeForLevel(prismLevel);

            var record = new ConquestTerritoryDAO
            {
                SubAreaId = subAreaId,
                AlignmentId = ALIGNMENT_NEUTRAL,
                BonusType = 0,
                Life = prismLife,
                MaxLife = prismLife,
                State = 0,
                PrismLevel = prismLevel,
                PrismType = (int)GetPrismTypeForSubArea(subAreaId),
            };

            ApplyVillageDefaults(record);
            var territory = new ConquestTerritory(record, persisted: false);
            m_bySubArea[subAreaId] = territory;

            if (record.PrismMapId > 0)
                m_prismMapBySubArea[subAreaId] = record.PrismMapId;
            return territory;
        }

        public void TerritoryDefended(ConquestTerritory territory, MapInstance map)
        {
            if (territory == null || map == null)
                return;

            WorldService.Instance.AddMessage(() => WorldService.Instance.Dispatcher.Dispatch(WorldMessage.CONQUEST_PRISM_SURVIVED(map)));
        }

        public void TerritoryDefeated(ConquestTerritory territory, MapInstance map)
        {
            if (territory == null)
                return;

            if (map != null)
            {
                WorldService.Instance.AddMessage(() => WorldService.Instance.Dispatcher.Dispatch(WorldMessage.CONQUEST_PRISM_DEAD(map)));
            }

            RemoveTerritory(territory.SubAreaId);

            WorldService.Instance.AddMessage(() => WorldService.Instance.Dispatcher.Dispatch(WorldMessage.IM_PVP_MESSAGE(InformationEnum.PVP_PRISM_DESTROYED)));
        }

        public void TerritoryAttacked(ConquestTerritory territory, MapInstance map)
        {
            if (territory == null || map == null)
                return;

            m_prismMapBySubArea[territory.SubAreaId] = map.Id;
            WorldService.Instance.AddMessage(() => WorldService.Instance.Dispatcher.Dispatch(WorldMessage.CONQUEST_PRISM_ATTACKED(map)));
        }

        public string SerializeAs_SubAreaAlignmentList()
        {
            var entries = new List<string>();

            foreach (var subArea in AreaManager.Instance.SubAreas
                .Where(s => s.CanConquest || s.DefaultAlignment != 0)
                .OrderBy(s => s.Id))
            {
                entries.Add(subArea.Id + ";" + GetAlignmentForSubArea(subArea.Id));
            }

            return string.Join("|", entries);
        }

        public string SerializeAs_WorldData(CharacterEntity character)
        {
            var alignmentId = character?.AlignmentId ?? ALIGNMENT_NEUTRAL;
            var worldZones = GetWorldZoneSubAreas().ToArray();
            var areaEntries = worldZones.OrderBy(s => s.Id).Select(s => SerializeWorldZone(s, alignmentId)).ToArray();

            var villageEntries = s_villages
                .Where(village => AreaManager.Instance.TryGetArea(village.AreaId, out var area))
                .OrderBy(village => village.PrismSubAreaId)
                .Select(SerializeVillage)
                .ToArray();

            var ownedAreas = worldZones.Count(s => GetAlignmentForSubArea(s.Id) == alignmentId);
            var possibleAreas = worldZones.Count(s => GetAlignmentForSubArea(s.Id) == ALIGNMENT_NEUTRAL);
            var ownedVillages = s_villages.Count(village => GetAlignmentForArea(village.AreaId) == alignmentId);

            var sb = new StringBuilder();
            sb.Append(ownedAreas).Append('|');
            sb.Append(areaEntries.Length).Append('|');
            sb.Append(possibleAreas).Append('|');
            sb.Append(string.Join(";", areaEntries)).Append('|');
            sb.Append(ownedVillages).Append('|');
            sb.Append(villageEntries.Length).Append('|');
            sb.Append(string.Join(";", villageEntries));
            return sb.ToString();
        }

        public int GetWorldBalance(int alignmentId)
        {
            var zones = GetWorldZoneSubAreas().ToArray();
            if (zones.Length == 0)
                return 0;

            return (int)Math.Round((zones.Count(s => GetAlignmentForSubArea(s.Id) == alignmentId) * 100.0) / zones.Length);
        }

        public int GetAreaBalance(int areaId, int alignmentId)
        {
            var zones = AreaManager.Instance.SubAreas
                .Where(s => s.Area.Id == areaId && IsConquerableSubArea(s))
                .ToArray();
            if (zones.Length == 0)
                return 0;

            return (int)Math.Round((zones.Count(s => GetAlignmentForSubArea(s.Id) == alignmentId) * 100.0) / zones.Length);
        }

        public int GetRankMultiplicator(CharacterEntity character)
        {
            if (character == null)
                return 1;

            return Math.Max(1, (int)Math.Round((character.AlignmentLevel / 2.5) + 1));
        }

        public bool CanAttack(ConquestTerritory territory, CharacterEntity character)
        {
            if (territory == null || !territory.CanAttack(character))
                return false;

            if (HasAdjacentAlly(territory.SubAreaId, character.AlignmentId))
                return true;

            return !m_bySubArea.Values.Any(t => t.AlignmentId == character.AlignmentId);
        }

        public int GetVillagePrismTemplateId(int subAreaId, int alignmentId)
        {
            var resolvedId = ResolveTerritorySubAreaId(subAreaId);
            if (s_villageByTerritorySubArea.TryGetValue(resolvedId, out var village))
                return village.GetTemplateId(alignmentId);
            if (s_villageByPrismSubArea.TryGetValue(subAreaId, out village))
                return village.GetTemplateId(alignmentId);
            return 0;
        }

        public ConquestPrismEntity CreatePrismEntityForMap(Game.Map.MapInstance map)
        {
            if (map == null)
                return null;

            ConquestTerritory territory = null;
            int cellId = -1;

            if (s_villageByRoomMap.TryGetValue(map.Id, out var village))
            {
                territory = GetOrCreateNeutralForAttack(village.TerritorySubAreaId);
                if (territory != null)
                {
                    territory.SetPrismType(ConquestPrismType.Village);
                    territory.SetPrismPosition(village.PrismMapId, village.PrismCellId);
                    cellId = village.PrismCellId;
                    m_prismMapBySubArea[territory.SubAreaId] = village.PrismMapId;
                }
            }
            else
            {
                territory = m_bySubArea.Values.FirstOrDefault(candidate =>
                    candidate.PrismMapId == map.Id && candidate.PrismCellId >= 0);
                if (territory != null)
                    cellId = territory.PrismCellId;
            }

            if (territory == null || cellId < 0)
                return null;

            return new ConquestPrismEntity(GetPrismEntityId(territory.SubAreaId), territory, map.Id, cellId);
        }

        public ConquestPrismEntity CreatePrismEntityForFight(Game.Map.MapInstance map, ConquestTerritory territory)
        {
            if (map == null || territory == null)
                return null;

            var mapPrism = CreatePrismEntityForMap(map);
            if (mapPrism != null)
            {
                if (mapPrism.Territory?.SubAreaId == territory.SubAreaId)
                    return mapPrism;

                mapPrism.Dispose();
            }

            var cellId = territory.PrismMapId == map.Id && territory.PrismCellId >= 0
                ? territory.PrismCellId
                : map.RandomFreeCell;

            if (cellId < 0)
                return null;

            if (territory.PrismMapId <= 0)
                territory.SetPrismPosition(map.Id, cellId);

            m_prismMapBySubArea[territory.SubAreaId] = map.Id;
            return new ConquestPrismEntity(GetPrismEntityId(territory.SubAreaId), territory, map.Id, cellId);
        }

        private IEnumerable<SubAreaInstance> GetWorldZoneSubAreas()
        {
            var ids = new HashSet<int>();

            foreach (var subArea in AreaManager.Instance.SubAreas)
            {
                if (subArea.Area == null)
                    continue;
                if (s_villageAreaIds.Contains(subArea.Area.Id))
                    continue;
                if (s_villageByPrismSubArea.ContainsKey(subArea.Id))
                    continue;
                if (subArea.CanConquest || subArea.DefaultAlignment != 0)
                    ids.Add(subArea.Id);
            }

            foreach (var territory in m_bySubArea.Values)
            {
                if (AreaManager.Instance.TryGetSubArea(territory.SubAreaId, out var subArea) &&
                    subArea.Area != null && !s_villageAreaIds.Contains(subArea.Area.Id))
                {
                    ids.Add(subArea.Id);
                }
            }

            foreach (var id in ids)
            {
                if (AreaManager.Instance.TryGetSubArea(id, out var subArea))
                    yield return subArea;
            }
        }

        private string SerializeWorldZone(SubAreaInstance subArea, int alignmentId)
        {
            var territory = GetBySubArea(subArea.Id) ?? (subArea.Area != null ? GetByArea(subArea.Area.Id) : null);
            var alignment = GetAlignmentForSubArea(subArea.Id);
            var fighting = territory != null && territory.IsUnderAttack ? 1 : 0;
            var prismMap = 0;
            if (territory != null)
            {
                if (territory.PrismMapId > 0)
                    prismMap = territory.PrismMapId;
                else if (m_prismMapBySubArea.TryGetValue(territory.SubAreaId, out var mapId))
                    prismMap = mapId;
            }
            var attackable = subArea.CanConquest && (territory == null || CanAttackForAlignment(territory, alignmentId)) ? 1 : 0;

            return subArea.Id + "," + NormalizeWorldAlignment(alignment) + "," + fighting + "," + prismMap + "," + attackable;
        }

        private string SerializeVillage(ConquestVillageDefinition village)
        {
            var territory = GetBySubArea(village.TerritorySubAreaId);
            var alignment = GetAlignmentForArea(village.AreaId);
            var door = territory != null && (territory.IsUnderAttack || territory.State == ConquestTerritoryStateEnum.STATE_VULNERABLE || territory.State == ConquestTerritoryStateEnum.STATE_DOOR_OPEN) ? 1 : 0;
            var prism = territory != null && (territory.IsUnderAttack || territory.State == ConquestTerritoryStateEnum.STATE_VULNERABLE || territory.State == ConquestTerritoryStateEnum.STATE_PRISM_ROOM_OPEN) ? 1 : 0;

            // First field must be the areaId (20/21/22/23/13/14) — client maps villages by area, not by prism subArea
            return village.AreaId + "," + NormalizeWorldAlignment(alignment) + "," + door + "," + prism;
        }

        private int GetAlignmentForSubArea(int subAreaId)
        {
            var territory = GetBySubArea(subAreaId);
            if (territory != null)
                return territory.AlignmentId;

            if (!AreaManager.Instance.TryGetSubArea(subAreaId, out var subArea))
                return ALIGNMENT_NEUTRAL;

            if (subArea.Area != null && IsVillageArea(subArea.Area.Id))
                return GetAlignmentForArea(subArea.Area.Id);

            // Conquerable subareas with no active territory are always neutral
            if (subArea.CanConquest)
                return ALIGNMENT_NEUTRAL;

            if (subArea.DefaultAlignment != 0)
                return subArea.DefaultAlignment;

            return ALIGNMENT_NEUTRAL;
        }

        /// <summary>
        /// Returns the alignment value that monster groups on this subarea should use.
        /// Only city/village areas (Bonta, Brakmar, Pandala cities) and permanently-aligned
        /// sub-areas have alignment militia. Wild monsters in regular conquered territories
        /// remain neutral so PvM fights start normally via the ready button.
        /// </summary>
        public int GetMonsterGroupAlignment(int subAreaId)
        {
            if (!AreaManager.Instance.TryGetSubArea(subAreaId, out var subArea))
                return -1;

            // Inherently aligned sub-areas (inner-city zones, always Bonta/Brakmar).
            if (subArea.DefaultAlignment != 0)
                return subArea.DefaultAlignment;

            // Village/city areas that can be conquered (Bonta, Brakmar, Pandala cities).
            // Militia in these areas carry the territory's alignment.
            if (subArea.Area != null && IsVillageArea(subArea.Area.Id))
            {
                int alignment = GetAlignmentForSubArea(subAreaId);
                return alignment == ALIGNMENT_NEUTRAL ? -1 : alignment;
            }

            // Regular conquerable territories: wild monsters are always neutral PvM.
            return -1;
        }

        private int GetAlignmentForArea(int areaId)
        {
            var territory = GetByArea(areaId);
            return territory?.AlignmentId ?? ALIGNMENT_NEUTRAL;
        }

        private int NormalizeWorldAlignment(int alignmentId)
        {
            return alignmentId == ALIGNMENT_NEUTRAL ? ALIGNMENT_NONE : alignmentId;
        }

        public static bool IsVillageArea(int areaId)
        {
            return s_villageAreaIds.Contains(areaId);
        }

        private static long GetPrismEntityId(int subAreaId)
        {
            return -100000L - subAreaId;
        }

        private static int ResolveTerritorySubAreaId(int subAreaId)
        {
            if (s_villageByPrismSubArea.TryGetValue(subAreaId, out var village))
                return village.TerritorySubAreaId;

            return subAreaId;
        }

        private static ConquestPrismType GetPrismTypeForSubArea(int subAreaId)
        {
            return s_villageByTerritorySubArea.ContainsKey(subAreaId)
                ? ConquestPrismType.Village
                : ConquestPrismType.SubArea;
        }

        private static void ApplyVillageDefaults(ConquestTerritoryDAO record)
        {
            if (record == null)
                return;

            if (record.PrismLevel <= 0)
                record.PrismLevel = ConquestPrismEntity.DefaultLevel;

            var maxLife = ConquestPrismEntity.GetMaxLifeForLevel(record.PrismLevel);
            if (record.MaxLife <= 0 || record.MaxLife == 3000)
                record.MaxLife = maxLife;
            if (record.Life <= 0 || record.Life > record.MaxLife)
                record.Life = record.MaxLife;

            if (s_villageByTerritorySubArea.TryGetValue(record.SubAreaId, out var village))
            {
                record.PrismType = (int)ConquestPrismType.Village;
                if (village.HasPrismRoom && record.PrismMapId <= 0)
                {
                    record.PrismMapId = village.PrismMapId;
                    record.PrismCellId = village.PrismCellId;
                }
            }
            else if (record.PrismType != (int)ConquestPrismType.Village)
            {
                record.PrismType = (int)ConquestPrismType.SubArea;
            }
        }

        private bool HasAdjacentAlly(int subAreaId, int alignmentId)
        {
            subAreaId = ResolveTerritorySubAreaId(subAreaId);
            if (!s_nearSubAreas.TryGetValue(subAreaId, out var nearSubAreas))
                return true;

            return nearSubAreas.Any(id => GetAlignmentForSubArea(id) == alignmentId);
        }

        private bool CanAttackForAlignment(ConquestTerritory territory, int alignmentId)
        {
            if (territory == null || territory.IsUnderAttack)
                return false;
            if (alignmentId <= ALIGNMENT_NEUTRAL || territory.AlignmentId == alignmentId)
                return false;

            if (HasAdjacentAlly(territory.SubAreaId, alignmentId))
                return true;

            return !m_bySubArea.Values.Any(t => t.AlignmentId == alignmentId);
        }

        private void DispatchAlignmentChanged(ConquestTerritory territory, bool silent)
        {
            if (territory == null)
                return;

            var subAreaId = territory.SubAreaId;
            var alignmentId = territory.AlignmentId;
            AreaManager.Instance.TryGetSubArea(subAreaId, out var subArea);
            var areaId = subArea?.Area?.Id ?? -1;

            WorldService.Instance.AddMessage(() =>
            {
                WorldService.Instance.Dispatcher.Dispatch(WorldMessage.SUBAREA_ALIGNMENT_CHANGED(subAreaId, alignmentId, silent));
                if (areaId > 0)
                    WorldService.Instance.Dispatcher.Dispatch(WorldMessage.AREA_ALIGNMENT_CHANGED(areaId, alignmentId));
            });

            BroadcastConquestWorldData();
        }

        private void BroadcastConquestWorldData()
        {
            WorldService.Instance.AddMessage(() =>
            {
                //foreach (var character in EntityManager.Instance.OnlineCharacters)
                    //character.SafeDispatch(WorldMessage.CONQUEST_WORLD_DATA(SerializeAs_WorldData(character)));
            });
        }
    }
}


