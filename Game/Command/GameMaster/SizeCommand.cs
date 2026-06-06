using Game.Network;

namespace Game.Command
{
    public sealed class SizeCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "tamano", "size"
        };

        public override string[] Aliases => _aliases;

        public override string Description => "Cambia el tamano visual de tu personaje. Uso: %size%";

        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            int size = 0;
            if (int.TryParse(context.TextCommandArgument.NextWord(), out size))
            {
                context.Character.DatabaseRecord.SkinSize = size;
                context.Character.RefreshOnMap();
            }
            else
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Formato: tamano %size%"));
            }
        }
    }
}
