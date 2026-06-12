using Protocolo.RPC.Service;

namespace Protocolo.RPC.Protocol
{
    public sealed class GameTicketMessage : AbstractRcpMessage
    {
        public override int Id
        {
            get
            {
                return (int)MessageIdEnum.AUTH_TO_WORLD_GAME_TICKET;
            }
        }

        public long AccountId
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Pseudo
        {
            get;
            private set;
        }

        public int Power
        {
            get;
            private set;
        }

        public long RemainingSubscription
        {
            get;
            private set;
        }

        public long LastConnectionDate
        {
            get;
            private set;
        }

        public string LastConnectionIP
        {
            get;
            private set;
        }

        public string Ticket
        {
            get;
            private set;
        }

        public GameTicketMessage(long accountId, string name, string pseudo, int power, long remainingSub, long lastConnection, string lastIp, string ticket)
        {
            AccountId = accountId;
            Name = name;
            Pseudo = pseudo;
            Power = power;
            RemainingSubscription = remainingSub;
            LastConnectionDate = lastConnection;
            LastConnectionIP = lastIp;
            Ticket = ticket;
        }

        public GameTicketMessage()
        {
        }

        public override void Deserialize()
        {
            AccountId = base.ReadLong();
            Name = base.ReadString();
            Pseudo = base.ReadString();
            Power = base.ReadInt();
            RemainingSubscription = base.ReadLong();
            LastConnectionDate = base.ReadLong();
            LastConnectionIP = base.ReadString();
            Ticket = base.ReadString();
        }

        public override void Serialize()
        {
            base.WriteLong(AccountId);
            base.WriteString(Name);
            base.WriteString(Pseudo);
            base.WriteInt(Power);
            base.WriteLong(RemainingSubscription);
            base.WriteLong(LastConnectionDate);
            base.WriteString(LastConnectionIP);
            base.WriteString(Ticket);
        }
    }
}
