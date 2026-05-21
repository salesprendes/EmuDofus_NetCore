using Protocolo.Framework.Network;
using Game.Database.Structure;
using Game.Entity;
using System.Collections.Generic;
using Game.Manager;

namespace Game.Network
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class WorldClient : AbstractDofusClient<WorldClient>
    {
        /// <summary>
        /// 
        /// </summary>
        public AccountTicket Account
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<CharacterDAO> Characters
        {
            get;
            set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        private CharacterEntity m_currentCharacter;

        /// <summary>
        /// 
        /// </summary>
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


