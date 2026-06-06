namespace Protocolo.Framework.Command
{
    public abstract class CommandContext
    {
        public TextCommandArgument TextCommandArgument { get; }

        protected CommandContext(string line)
        {
            TextCommandArgument = new TextCommandArgument(line);
        }
    }
}
