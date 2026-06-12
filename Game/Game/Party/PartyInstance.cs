using Game.Entity;
using Game.Manager;
using Game.Network;
using System.Collections.Generic;
using System.Linq;

namespace Game.Party
{
    public sealed class PartyInstance : MessageDispatcher
    {
        public long Id
        {
            get;
        }

        public int MemberCount => m_memberById.Count;

        private CharacterEntity m_leader;
        private Dictionary<long, CharacterEntity> m_memberById;

        public PartyInstance(long id, CharacterEntity master, CharacterEntity member)
        {
            Id = id;
            m_memberById = new Dictionary<long, CharacterEntity>();
            m_leader = master;

            AddMember(master);
            AddMember(member);
        }

        public void KickMember(CharacterEntity member, long memberId)
        {

            if (member.Id != m_leader.Id || member.Id == memberId)
            {
                member.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }


            if (!m_memberById.ContainsKey(memberId))
            {
                member.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }


            RemoveMember(m_memberById[memberId], member.Id.ToString());
        }

        public void AddMember(CharacterEntity member)
        {

            Dispatch(WorldMessage.PARTY_MEMBER_LIST(member));

            m_memberById.Add(member.Id, member);
            AddHandler(member.SafeDispatch);


            member.PartyId = Id;
            member.SafeDispatch(WorldMessage.PARTY_CREATE_SUCCESS(m_leader.Name));
            member.SafeDispatch(WorldMessage.PARTY_SET_LEADER(m_leader.Id));
            member.SafeDispatch(WorldMessage.PARTY_MEMBER_LIST(m_memberById.Values.ToArray()));
        }

        public void RemoveMember(CharacterEntity member, string kickerId = "")
        {
            if (m_memberById.ContainsKey(member.Id))
            {
                member.PartyId = -1;
                m_memberById.Remove(member.Id);
                RemoveHandler(member.SafeDispatch);

                Dispatch(WorldMessage.PARTY_MEMBER_LEFT(member.Id));

                member.SafeDispatch(WorldMessage.PARTY_LEAVE(kickerId));

                if (m_memberById.Count == 1)
                {
                    Dispose();
                }
                else if (member.Id == m_leader.Id)
                {
                    m_leader = m_memberById.First().Value;
                    Dispatch(WorldMessage.PARTY_SET_LEADER(m_leader.Id));
                }
            }
        }

        public override void Dispose()
        {
            Dispatch(WorldMessage.PARTY_LEAVE());

            foreach (var member in m_memberById.Values)
                member.PartyId = -1;

            m_memberById.Clear();
            m_memberById = null;
            m_leader = null;

            PartyManager.Instance.RemoveParty(this);

            base.Dispose();
        }
    }
}


