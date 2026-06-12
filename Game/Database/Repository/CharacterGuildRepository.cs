using Protocolo.Framework.Database;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class CharacterGuildRepository : Repository<CharacterGuildRepository, CharacterGuildDAO>
    {
        private Dictionary<long, CharacterGuildDAO> m_characterGuildById;

        public CharacterGuildRepository()
        {
            m_characterGuildById = new Dictionary<long, CharacterGuildDAO>();
        }

        public CharacterGuildDAO GetById(long id)
        {
            if (m_characterGuildById.ContainsKey(id))
                return m_characterGuildById[id];
            return base.Load("Id=@Id", new { Id = id });
        }

        public override void OnObjectAdded(CharacterGuildDAO characterGuild)
        {
            m_characterGuildById.Add(characterGuild.Id, characterGuild);
        }

        public override void OnObjectRemoved(CharacterGuildDAO characterGuild)
        {
            m_characterGuildById.Remove(characterGuild.Id);
        }
    }
}

