using Protocolo.RPC.Service;
using System.Collections.Generic;

namespace Protocolo.RPC.Protocol
{
    public sealed class AccountConnectedList : AbstractRcpMessage
    {
        /// <summary>
        /// 
        /// </summary>
        public override int Id
        {
            get 
            { 
                return (int)MessageIdEnum.WORLD_TO_AUTH_ACCOUNT_CONNECTED_LIST;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<long> ConnectedAccounts
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountId"></param>
        public AccountConnectedList(IEnumerable<long> connectedAccounts)
        {
            ConnectedAccounts = new List<long>(connectedAccounts);
        }

        /// <summary>
        /// 
        /// </summary>
        public AccountConnectedList()
        {
            ConnectedAccounts = new List<long>();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Deserialize()
        {
            long length = base.ReadLong();
            for(long i = 0; i < length; i++)
                ConnectedAccounts.Add(base.ReadLong());
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Serialize()
        {
            base.WriteLong(ConnectedAccounts.Count);

            foreach(long connectedAccount in ConnectedAccounts)
                base.WriteLong(connectedAccount);
        }
    }
}
