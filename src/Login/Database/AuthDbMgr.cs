using Login.Database.Repository;
using Protocolo.Framework.Configuration;
using Protocolo.Framework.Database;

namespace Login.Database
{
    public sealed class AuthDbMgr : DbManager<AuthDbMgr>
    {
        [Configurable("DbConnection")]
        public static string DbConnection = "Server=localhost;Database=login_emudofus;Uid=root;Pwd=;SslMode=Disabled;Convert Zero Datetime=True;Allow Zero Datetime=True;";

        public void Initialize()
        {
            base.AddRepository(AccountRepository.Instance);
            base.AddRepository(CharacterInstanceRepository.Instance);
            base.AddRepository(GameServerRepository.Instance);
            base.LoadAll(DbConnection);
        }
    }
}
