namespace Game.Command
{
    public sealed class AlignmentResetCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "resetalineamiento", "alignmentreset" };

        public override string[] Aliases => _aliases;

        public override string Description => "Restablece el alineamiento de tu personaje.";

        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            context.Character.ResetAlignment();
        }
    }
}
