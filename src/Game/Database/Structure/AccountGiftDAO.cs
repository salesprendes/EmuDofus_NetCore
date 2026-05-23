using Protocolo.Framework.Database;
using PropertyChanged;

namespace Game.Database.Structure
{
    /// <summary>
    /// Representa un regalo pendiente para una cuenta.
    /// Los items se almacenan como "templateId:quantity" separados por "|".
    /// Ejemplo: "12345:5|67890:1"
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    [Table("account_gift")]
    public class AccountGiftDAO : DataAccessObject<AccountGiftDAO>
    {
        [Key]
        public int Id { get; set; }

        public long AccountId { get; set; }

        // Tipo de regalo (1 = items estandar)
        public int GiftType { get; set; }

        // Texto visible en la UI del cliente (URL-encoded)
        public string Title { get; set; }

        public string Description { get; set; }

        // URL de la imagen del regalo (URL-encoded)
        public string GfxUrl { get; set; }

        // Items en formato "templateId:quantity|templateId:quantity"
        public string Items { get; set; }
    }
}
