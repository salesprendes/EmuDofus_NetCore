namespace Game.Command
{
    public sealed partial class PacketCommand : WorldStaffCommand
    {
        private static readonly string[] m_aliases = { "packet" };

        public override string[] Aliases => m_aliases;

        public override string Description => "Comandos para probar y enviar paquetes.";

        protected override StaffRole RequiredRole => StaffRole.Administrator;

        protected override void Process(WorldCommandContext context)
        {
            base.Process(context);
        }
    }
}
