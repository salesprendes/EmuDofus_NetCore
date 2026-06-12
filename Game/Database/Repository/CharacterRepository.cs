using Protocolo.Framework.Database;
using Game.Database.Structure;
using Game.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Repository
{
    public sealed class CharacterRepository : Repository<CharacterRepository, CharacterDAO>
    {
        public long NextCharacterId
        {
            get
            {
                lock (m_syncLock)
                    return m_nextCharacterId++;
            }
        }

        private long m_nextCharacterId;

        private Dictionary<long, CharacterDAO> m_characterById;

        private Dictionary<string, CharacterDAO> m_characterByName;

        private Dictionary<long, List<CharacterDAO>> m_charactersByAccount;

        public CharacterRepository()
        {
            m_characterById = new Dictionary<long, CharacterDAO>();
            m_characterByName = new Dictionary<string, CharacterDAO>();
            m_charactersByAccount = new Dictionary<long, List<CharacterDAO>>();
            m_nextCharacterId = 10000;
        }

        public CharacterDAO GetById(long characterId)
        {
            if (m_characterById.ContainsKey(characterId))
                return m_characterById[characterId];
            return base.Load("Id=@Id", new { Id = characterId }); ;
        }

        public CharacterDAO GetByName(string name)
        {
            if (m_characterByName.ContainsKey(name.ToLower()))
                return m_characterByName[name.ToLower()];
            return base.Load("upper(Name)=upper(@Name)", new { Name = name });
        }

        public List<CharacterDAO> GetByAccount(long accountId)
        {
            List<CharacterDAO> characters = new List<CharacterDAO>();
            if (m_charactersByAccount.ContainsKey(accountId))
                characters.AddRange(m_charactersByAccount[accountId]);
            else
                characters.AddRange(base.LoadMultiple("AccountId=@AccountId", new { AccountId = accountId }));
            return characters;
        }

        public override void OnObjectAdded(CharacterDAO character)
        {
            m_characterById.Add(character.Id, character);
            m_characterByName.Add(character.Name.ToLower(), character);

            if (!m_charactersByAccount.ContainsKey(character.AccountId))
                m_charactersByAccount.Add(character.AccountId, new List<CharacterDAO>());
            m_charactersByAccount[character.AccountId].Add(character);

            if (character.Id >= m_nextCharacterId)
                m_nextCharacterId = character.Id + 1;
        }

        public override void OnObjectRemoved(CharacterDAO character)
        {
            m_characterById.Remove(character.Id);
            m_characterByName.Remove(character.Name.ToLower());
            m_charactersByAccount[character.AccountId].Remove(character);
        }

        public int GetCountByAccount(long accountId)
        {
            List<CharacterDAO> list;
            return m_charactersByAccount.TryGetValue(accountId, out list) ? list.Count : 0;
        }

        public Dictionary<long, int> GetAllCounts()
        {
            var result = new Dictionary<long, int>(m_charactersByAccount.Count);
            foreach (var pair in m_charactersByAccount)
                if (pair.Value.Count > 0)
                    result[pair.Key] = pair.Value.Count;
            return result;
        }

        public IEnumerable<CharacterDAO> GetAllCharacters()
        {
            lock (m_syncLock)
                return m_characterById.Values.ToArray();
        }
    }
}
