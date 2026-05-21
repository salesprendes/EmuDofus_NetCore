namespace Game.Conquest
{
    public enum ConquestPrismType
    {
        SubArea = 0,
        Village = 1,
    }

    public sealed class ConquestVillageDefinition
    {
        public int AreaId { get; private set; }
        public int TerritorySubAreaId { get; private set; }
        public int PrismSubAreaId { get; private set; }
        public int PrismMapId { get; private set; }
        public int PrismCellId { get; private set; }
        public int BontaTemplateId { get; private set; }
        public int BrakmarTemplateId { get; private set; }

        public bool HasPrismRoom => PrismMapId > 0 && PrismCellId >= 0;

        public ConquestVillageDefinition(int areaId, int territorySubAreaId, int prismSubAreaId,
            int prismMapId = 0, int prismCellId = -1,
            int bontaTemplateId = 0, int brakmarTemplateId = 0)
        {
            AreaId = areaId;
            TerritorySubAreaId = territorySubAreaId;
            PrismSubAreaId = prismSubAreaId;
            PrismMapId = prismMapId;
            PrismCellId = prismCellId;
            BontaTemplateId = bontaTemplateId;
            BrakmarTemplateId = brakmarTemplateId;
        }

        public int GetTemplateId(int alignmentId)
        {
            if (alignmentId == 2) return BrakmarTemplateId;
            if (alignmentId == 1) return BontaTemplateId;
            // Neutral: return Bonta template as visual default
            return BontaTemplateId;
        }
    }
}
