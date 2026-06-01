using Protocolo.Framework.Database;
using Game.Database.Structure;
using MySqlConnector;

namespace Game.Database.Repository
{
    public sealed class SpellRepository : Repository<SpellRepository, SpellDAO>
    {
        public override void UpdateAll(MySqlConnection connection, MySqlTransaction transaction) { }
        public override void DeleteAll(MySqlConnection connection, MySqlTransaction transaction) { }
        public override void InsertAll(MySqlConnection connection, MySqlTransaction transaction) { }
    }
}
