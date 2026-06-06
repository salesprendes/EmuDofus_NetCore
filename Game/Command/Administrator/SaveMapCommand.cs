using Game.Database.Repository;
using Game.Network;

namespace Game.Command
{
    public sealed partial class WorldCommand
    {
        public sealed class SaveMapCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "savemap"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Guarda los cambios del mapa actual.";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                MapTemplateRepository.Instance.GetById(context.Character.MapId).Update();

                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Map saved."));
            }
        }
    }
}
