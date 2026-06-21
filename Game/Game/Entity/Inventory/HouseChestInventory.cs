using Game.Database.Structure;

namespace Game.Entity.Inventory
{
    public sealed class HouseChestInventory : StorageInventory
    {
        private readonly HouseDAO m_record;

        public override long Kamas
        {
            get => m_record.ChestKamas;
            set => m_record.ChestKamas = value;
        }

        public HouseChestInventory(HouseDAO record)
            : base((int)EntityTypeEnum.TYPE_HOUSE_CHEST, record.Id)
        {
            m_record = record;
        }
    }
}
