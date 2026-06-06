using Game.Network;

namespace Game.Command
{
    public sealed partial class WorldCommand
    {
        public sealed class SaveWorldCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "save"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Guarda todos los datos del mundo.";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                WorldService.Instance.SaveWorld();

                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("World saved."));
            }
        }
    }
}
