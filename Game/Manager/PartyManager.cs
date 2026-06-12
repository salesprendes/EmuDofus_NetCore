using Protocolo.Framework.Generic;
using Game;
using Game.Entity;
using Game.Party;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Manager
{
    public sealed class PartyManager : Singleton<PartyManager>
    {
        private long m_nextPartyId;
        private readonly Dictionary<long, PartyInstance> m_partyById;

        public PartyManager()
        {
            m_partyById = new Dictionary<long, PartyInstance>();
        }

        public void PartyMessage(long partyId, string message)
        {
            WorldService.Instance.AddMessage(() => { var party = GetParty(partyId); if (party != null) party.Dispatch(message); });
        }

        public void PartyLeave(CharacterEntity character)
        {
            if (m_partyById.ContainsKey(character.PartyId))
                m_partyById[character.PartyId].RemoveMember(character);
        }

        public void CreateParty(CharacterEntity master, CharacterEntity member)
        {
            var party = new PartyInstance(m_nextPartyId++, master, member);

            m_partyById.Add(party.Id, party);

            WorldService.Instance.AddUpdatable(party);
        }

        public void RemoveParty(PartyInstance instance)
        {
            m_partyById.Remove(instance.Id);

            WorldService.Instance.RemoveUpdatable(instance);
        }

        public PartyInstance GetParty(long partyId)
        {
            if (m_partyById.ContainsKey(partyId))
                return m_partyById[partyId];
            return null;
        }
    }
}



