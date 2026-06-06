namespace Game.Command
{
    public sealed partial class PacketCommand
    {
        public sealed class PacketSendCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "send"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Envia un paquete raw al cliente. Uso: %rawString%";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                context.Character.Dispatch(context.TextCommandArgument.NextWord());
            }
        }
    }
}
