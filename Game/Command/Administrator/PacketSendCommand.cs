namespace Game.Command
{
    public sealed class PacketSendCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "paquete", "packet", "send"
        };

        public override string[] Aliases => _aliases;

        public override string Description => "Envia un paquete raw al cliente. Uso: %rawString%";

        protected override StaffRole RequiredRole => StaffRole.Administrator;

        protected override void Process(WorldCommandContext context)
        {
            var packet = context.TextCommandArgument.ReadRemainingText();
            if (string.IsNullOrEmpty(packet))
            {
                context.Character.Dispatch(Game.Network.WorldMessage.BASIC_CONSOLE_MESSAGE("Formato: paquete %rawString%"));
                return;
            }

            context.Character.Dispatch(packet);
        }
    }
}
