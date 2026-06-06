using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class AlignmentCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "alignment"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Cambia el alineamiento de tu personaje. Uso: %alignmentId%";

            protected override StaffRole RequiredRole => StaffRole.GameMaster;

            protected override void Process(WorldCommandContext context)
            {
                var alignmentId = -1;
                if (!int.TryParse(context.TextCommandArgument.NextWord(), out alignmentId))
                {
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Command format : character alignment %alignementId%"));
                    return;
                }

                context.Character.SetAlignment(alignmentId);
            }
        }
    }
}
