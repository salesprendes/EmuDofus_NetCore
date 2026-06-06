using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "character"
        };

        public override string[] Aliases => _aliases;

        public override string Description => "Comandos para gestionar personajes.";

        protected override StaffRole RequiredRole => StaffRole.Moderator;

        protected override void Process(WorldCommandContext context)
        {
            context.Character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
        }
    }
}
