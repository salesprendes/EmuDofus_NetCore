using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class AuthentificationMessage : AbstractRcpMessage
    {
        public override int Id
        {
            get
            {
                return (int)MessageIdEnum.WORLD_TO_AUTH_CREDENTIAL;
            }
        }

        public string Password
        {
            get;
            private set;
        }

        public string RemoteIp
        {
            get;
            private set;
        }

        public AuthentificationMessage()
        {
        }

        public AuthentificationMessage(string password, string remoteIp)
        {
            Password = password;
            RemoteIp = remoteIp;
        }

        public override void Deserialize()
        {
            Password = base.ReadString();
            RemoteIp = base.ReadString();
        }

        public override void Serialize()
        {
            base.WriteString(Password);
            base.WriteString(RemoteIp);
        }
    }
}
