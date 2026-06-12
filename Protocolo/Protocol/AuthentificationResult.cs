using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class AuthentificationResult : AbstractRcpMessage
    {
        public override int Id
        {
            get
            {
                return (int)MessageIdEnum.AUTH_TO_WORLD_CREDENTIAL_RESULT;
            }
        }

        public AuthResultEnum Result
        {
            get;
            private set;
        }

        public AuthentificationResult()
        {
        }

        public AuthentificationResult(AuthResultEnum result)
        {
            Result = result;
        }

        public override void Deserialize()
        {
            Result = (AuthResultEnum)base.ReadInt();
        }

        public override void Serialize()
        {
            base.WriteInt((int)Result);
        }
    }
}
