namespace Game.Command
{
    public sealed partial class WorldCommand : WorldStaffCommand
    {
        private static readonly string[] m_aliases = { "world", "w" };

        public override string[] Aliases => m_aliases;

        public override string Description => "Comandos para administrar mapas y datos del mundo.";

        protected override StaffRole RequiredRole => StaffRole.Administrator;

        protected override void Process(WorldCommandContext context)
        {
            base.Process(context);
        }
    }
}
