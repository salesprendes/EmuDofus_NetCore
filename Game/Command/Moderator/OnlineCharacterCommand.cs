using Game.Manager;
using Game.Network;
using System.Linq;
using System.Text;

namespace Game.Command
{
    public sealed class OnlineCharacterCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "conectados", "online" };

        public override string[] Aliases => _aliases;

        public override string Description => "Muestra los jugadores conectados ahora mismo.";

        protected override StaffRole RequiredRole => StaffRole.Moderator;

        protected override void Process(WorldCommandContext context)
        {
            WorldService.Instance.AddMessage(() =>
            {
                var message = new StringBuilder("Online players " + ClientManager.Instance.Clients.Count() + " :\n");

                int i = 1;
                foreach (var client in ClientManager.Instance.Clients)
                {
                    if (client.CurrentCharacter != null)
                    {
                        message.Append(i++ + " : account(" + client.Account.Name + ") " + client.CurrentCharacter.Name + " map(" + client.CurrentCharacter.MapId + ") ip(" + client.Ip + ")\n");
                    }
                }

                context.Character.AddMessage(() => { context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE(message.ToString())); });
            });
        }
    }
}
