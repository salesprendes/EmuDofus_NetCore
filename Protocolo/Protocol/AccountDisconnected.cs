using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class AccountDisconnected : AbstractRcpMessage
    {
        public override int Id
        {
            get
            {
                return (int)MessageIdEnum.WORLD_TO_AUTH_ACCOUNT_DISCONNECTED;
            }
        }

        public long AccountId
        {
            get;
            private set;
        }

        public AccountDisconnected(long accountId)
        {
            AccountId = accountId;
        }

        public AccountDisconnected()
        {
        }

        public override void Deserialize()
        {
            AccountId = base.ReadLong();
        }

        public override void Serialize()
        {
            base.WriteLong(AccountId);
        }
    }
}
