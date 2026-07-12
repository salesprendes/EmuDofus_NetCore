namespace Game.Map
{
    public sealed class GroundItem
    {
        public int CellId { get; }
        public int TemplateId { get; }
        public int Quantity { get; set; }

        // Efectos/stats del objeto ya serializados (formato de BD). Se reconstruyen con
        // GenericStats.ParseFromString al recogerlo, para conservar exactamente las mismas
        // caracteristicas (forjas, tiradas...) sin depender del ItemDAO original.
        public string StringEffects { get; }

        public GroundItem(int cellId, int templateId, int quantity, string stringEffects)
        {
            CellId = cellId;
            TemplateId = templateId;
            Quantity = quantity;
            StringEffects = stringEffects ?? string.Empty;
        }
    }
}
