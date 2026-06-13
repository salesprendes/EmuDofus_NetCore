using Game.Network;
using System.Text;

namespace Game.Command
{
    public sealed class HelpCommand : WorldStaffCommand
    {
        private static readonly string[] m_aliases = ["help", "ayuda"];
        public override string[] Aliases => m_aliases;
        public override string Description => "Muestra los comandos que puedes usar.";
        protected override StaffRole RequiredRole => StaffRole.Moderator;

        protected override void Process(WorldCommandContext context)
        {
            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE(BuildHelpMessage(context)));
        }

        private static string BuildHelpMessage(WorldCommandContext context)
        {
            StringBuilder message = new StringBuilder();

            foreach (var command in WorldService.Instance.CommandManager.Commands)
                command.Serialize(message, context);

            return message.ToString();
        }
    }
}
