using Protocolo.Framework.Database;
namespace Game.Database.Structure
{
    [Table("account_gift")]
    public class AccountGiftDAO : DataAccessObject<AccountGiftDAO>
    {
        private int _id;
        private long _accountId;
        private int _giftType;
        private string _title;
        private string _description;
        private string _gfxUrl;
        private string _items;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public long AccountId
        {
            get => _accountId;
            set => SetProperty(ref _accountId, value);
        }


        public int GiftType
        {
            get => _giftType;
            set => SetProperty(ref _giftType, value);
        }


        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }


        public string GfxUrl
        {
            get => _gfxUrl;
            set => SetProperty(ref _gfxUrl, value);
        }


        public string Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }
    }
}
