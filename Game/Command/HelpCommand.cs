using Protocolo.Framework.Command;
using Game.Network;
using System.Text;

namespace Game.Command
{
    public sealed class HelpCommand : Command<WorldCommandContext>
    {
        private static readonly string[] m_aliases = { "help", "h" };

        public override string[] Aliases => m_aliases;

        public override string Description => "Lists the available commands.";

        protected override void Process(WorldCommandContext context)
        {
            StringBuilder message = new StringBuilder();
            foreach(var command in WorldService.Instance.CommandManager.Commands)  
                if(command.GetType().BaseType != typeof(SubCommand<WorldCommandContext>))
                    command.Serialize(message);
            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE(message.ToString()));
        }
    }
}

