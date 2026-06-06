namespace Game.Command
{
    public sealed class MonsterCommand : WorldStaffCommand
    {
        private static readonly string[] m_aliases = { "monster", "m" };

        public override string[] Aliases => m_aliases;

        public override string Description => "Comandos para gestionar monstruos.";

        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            base.Process(context);
        }
    }
}
