using Protocolo.Framework.Database;
using PropertyChanged;

namespace Game.Database.Structure
{
    [Table("bank")]
    [AddINotifyPropertyChangedInterface]
    public sealed class BankDAO : DataAccessObject<BankDAO>
    {
        [Key]
        public long Id
        {
            get;
            set;
        }

        public long Kamas
        {
            get;
            set;
        }
    }
}

