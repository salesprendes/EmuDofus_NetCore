namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class AlignmentResetCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "alignmentreset"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Restablece el alineamiento de tu personaje.";

            protected override StaffRole RequiredRole => StaffRole.GameMaster;

            protected override void Process(WorldCommandContext context)
            {
                context.Character.ResetAlignment();
            }
        }
    }
}
