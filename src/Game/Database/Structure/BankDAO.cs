using Protocolo.Framework.Database;
namespace Game.Database.Structure
{
    [Table("bank")]
    public sealed class BankDAO : DataAccessObject<BankDAO>
    {
        private long _id;
        private long _kamas;


        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public long Kamas
        {
            get => _kamas;
            set => SetProperty(ref _kamas, value);
        }
    }
}

