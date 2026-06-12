using Protocolo.Framework.Database;
using Game.Database.Structure;
using Game.Guild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class GuildRepository : Repository<GuildRepository, GuildDAO>
    {
        public long NextGuildId
        {
            get
            {
                lock (m_syncLock)
                    return m_nextGuildId++;
            }
        }

        private long m_nextGuildId;


        private Dictionary<long, GuildDAO> m_guildById;

        private Dictionary<string, GuildDAO> m_guildByName;

        public GuildRepository()
        {
            m_guildById = new Dictionary<long, GuildDAO>();
            m_guildByName = new Dictionary<string, GuildDAO>();
        }

        public GuildDAO GetByName(string name)
        {
            name = name.ToLower();
            if (m_guildByName.ContainsKey(name))
                return m_guildByName[name];
            return null;
        }

        public GuildDAO GetById(long id)
        {
            if (m_guildById.ContainsKey(id))
                return m_guildById[id];
            return null;
        }

        public override void OnObjectAdded(GuildDAO guild)
        {
            m_guildById.Add(guild.Id, guild);
            m_guildByName.Add(guild.Name.ToLower(), guild);

            if (guild.Id >= m_nextGuildId)
                m_nextGuildId = guild.Id + 1;
        }

        public override void OnObjectRemoved(GuildDAO guild)
        {
            m_guildById.Remove(guild.Id);
            m_guildByName.Remove(guild.Name.ToLower());
        }

        public GuildDAO Create(string name, int backgroundId, int backgroundColor, int symbolId, int symbolColor)
        {
            var instance = new GuildDAO()
            {
                Id = NextGuildId,
                Name = name,
                BackgroundId = backgroundId,
                BackgroundColor = backgroundColor,
                SymbolId = symbolId,
                SymbolColor = symbolColor,
                Level = 1,
                BoostPoint = 0,
                Experience = 0,
            };

            var stats = GuildStatistics.Create(instance);

            instance.Stats = stats.Serialize();

            base.Created(instance);

            return instance;
        }
    }
}


