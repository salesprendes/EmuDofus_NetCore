using Game.Database.Structure;
using Game.Entity;
using Game.Manager;
using Protocolo.Framework.Network;
using System.Collections.Generic;

namespace Game.Network
{
    public sealed class WorldClient : AbstractDofusClient<WorldClient>
    {
        public AccountTicket Account
        {
            get;
            set;
        }

        public List<CharacterDAO> Characters
        {
            get;
            set;
        }

        private CharacterEntity m_currentCharacter;

        public CharacterEntity CurrentCharacter
        {
            get
            {
                return m_currentCharacter;
            }
            set
            {
                if (m_currentCharacter != null)
                {
                    m_currentCharacter.RemoveHandler(Send);
                    m_currentCharacter.KickEvent -= Disconnect;
                }

                m_currentCharacter = value;
                if (m_currentCharacter != null)
                {
                    m_currentCharacter.Ip = Ip;
                    m_currentCharacter.AddHandler(Send);
                    m_currentCharacter.KickEvent += Disconnect;
                }
            }
        }
    }
}


