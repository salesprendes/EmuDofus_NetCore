using Protocolo.RPC.Service;
using System.Collections.Generic;
using System.IO;

namespace Protocolo.RPC.Protocol
{
    public sealed class AccountConnectedList : AbstractRcpMessage
    {
        private const int MaxConnectedAccounts = 100000;

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

            if (length < 0 || length > MaxConnectedAccounts)
                throw new InvalidDataException($"AccountConnectedList: longitud invalida: {length}");

            var count = (int)length;
            if (count > base.Count / sizeof(long))
                throw new InvalidDataException($"AccountConnectedList: datos insuficientes para la longitud declarada: {length}");

            var accounts = ConnectedAccounts;
            accounts.Clear();
            if (accounts.Capacity < count)
                accounts.Capacity = count;

            for (var i = 0; i < count; i++)
                accounts.Add(base.ReadLong());
        }

        public override void Serialize()
        {
            base.WriteLong(ConnectedAccounts.Count);

            foreach (long connectedAccount in ConnectedAccounts)
                base.WriteLong(connectedAccount);
        }
    }
}
