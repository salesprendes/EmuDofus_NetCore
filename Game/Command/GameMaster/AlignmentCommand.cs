using Game.Network;

namespace Game.Command
{
    public sealed class AlignmentCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "alineamiento", "alignment", "align" };
        public override string[] Aliases => _aliases;
        public override string Description => "Cambia el alineamiento de tu personaje. Uso: %alignmentId%";
        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            var alignmentId = -1;
            if (!int.TryParse(context.TextCommandArgument.NextWord(), out alignmentId))
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Formato: alineamiento %alignmentId%"));
                return;
            }

            context.Character.SetAlignment(alignmentId);
        }
    }
}
