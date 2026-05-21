using Protocolo.Framework.Database;
using PropertyChanged;

namespace Login.Database.Structure
{
    [Table("characterinstance")]
    [AddINotifyPropertyChangedInterface]
    public sealed class CharacterInstanceDAO : DataAccessObject<CharacterInstanceDAO>
    {
        [Key]
        public long Id { get; set; }

        public int ServerId { get; set; }

        public long AccountId { get; set; }
    }
}
