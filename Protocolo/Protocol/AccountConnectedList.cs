using Protocolo.RPC.Service;
using System.Collections.Generic;

namespace Protocolo.RPC.Protocol
{
    public sealed class AccountConnectedList : AbstractRcpMessage
    {
        public override int Id
        {
            get
            {
                return (int)MessageIdEnum.WORLD_TO_AUTH_ACCOUNT_CONNECTED_LIST;
            }
        }

        public List<long> ConnectedAccounts
        {
            get;
            private set;
        }

        public AccountConnectedList(IEnumerable<long> connectedAccounts)
        {
            ConnectedAccounts = new List<long>(connectedAccounts);
        }

        public AccountConnectedList()
        {
            ConnectedAccounts = new List<long>();
        }

        public override void Deserialize()
        {
            long length = base.ReadLong();
            for (long i = 0; i < length; i++)
                ConnectedAccounts.Add(base.ReadLong());
        }

        public override void Serialize()
        {
            base.WriteLong(ConnectedAccounts.Count);

            foreach (long connectedAccount in ConnectedAccounts)
                base.WriteLong(connectedAccount);
        }
    }
}
