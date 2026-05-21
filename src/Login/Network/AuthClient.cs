using Protocolo.Framework.Network;
using Login.Database.Structure;

namespace Login.Network
{
    public sealed class AuthClient : AbstractDofusClient<AuthClient>
    {
        public string AuthKey
        {
            get;
            set;
        }

        public string Ticket
        {
            get;
            set;
        }

        public AccountDAO Account
        {
            get;
            set;
        }

        public bool IsWaitingAuthenticationQueue
        {
            get;
            set;
        }
    }
}

