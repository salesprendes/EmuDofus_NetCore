using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity;
using Game.Guild;
using Game.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Manager
{
    public sealed class GuildManager : Singleton<GuildManager>
    {
        private readonly Dictionary<long, GuildInstance> m_guildById;

        private readonly Dictionary<string, GuildInstance> m_guildByName;

        public GuildManager()
        {
            m_guildById = new Dictionary<long, GuildInstance>();
            m_guildByName = new Dictionary<string, GuildInstance>();
        }

        public void Initialize()
        {
            foreach (var guild in GuildRepository.Instance.All)
            {
                AddInstance(new GuildInstance(guild));
            }
        }

        private void AddInstance(GuildInstance instance)
        {
            WorldService.Instance.AddUpdatable(instance);
            m_guildById.Add(instance.Id, instance);
            m_guildByName.Add(instance.Name.ToLower(), instance);
        }

        public GuildMember GetMember(long guildId, long memberId)
        {
            if (!m_guildById.ContainsKey(guildId))
                return null;
            return m_guildById[guildId].GetMember(memberId);
        }

        public GuildInstance GetGuild(long guildId)
        {
            if (m_guildById.ContainsKey(guildId))
                return m_guildById[guildId];
            return null;
        }

        public bool Create(CharacterEntity character, string name, int backgroundId, int backgroundColor, int symbolId, int symbolColor)
        {
            AddInstance(new GuildInstance(GuildRepository.Instance.Create(name, backgroundId, backgroundColor, symbolId, symbolColor), character));
            return true;
        }

        public void Destroy(GuildInstance guild)
        {
            m_guildById.Remove(guild.Id);
            m_guildByName.Remove(guild.Name.ToLower());
        }

        public bool Exists(string name)
        {
            return m_guildByName.ContainsKey(name.ToLower());
        }
    }
}


