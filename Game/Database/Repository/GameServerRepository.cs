using System.Collections.Generic;
using Protocolo.Framework.Database;
using Game.Database.Structure;

namespace Game.Database.Repository
{
    public sealed class GameServerRepository : Repository<GameServerRepository, GameServerDAO>
    {
        private readonly Dictionary<int, GameServerDAO> m_gameServerById;

        public GameServerRepository()
        {
            m_gameServerById = new Dictionary<int, GameServerDAO>();
        }

        public GameServerDAO GetById(int id)
        {
            GameServerDAO server;
            m_gameServerById.TryGetValue(id, out server);
            return server;
        }

        public override void OnObjectAdded(GameServerDAO server)
        {
            m_gameServerById[server.Id] = server;
        }

        public override void OnObjectRemoved(GameServerDAO server)
        {
            m_gameServerById.Remove(server.Id);
        }
    }
}
