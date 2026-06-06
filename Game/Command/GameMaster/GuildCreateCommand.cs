using Game.Action;
using Game.Network;

namespace Game.Command
{
    public sealed class GuildCreateCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "gremio", "guild"
        };

        public override string[] Aliases => _aliases;

        public override string Description => "Abre el panel para crear un gremio.";

        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            if (context.Character.CanGameAction(GameActionTypeEnum.GUILD_CREATE))
            {
                context.Character.GuildCreationOpen();
            }
            else
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("No puedes abrir la creacion de gremio en tu estado actual."));
            }
        }
    }
}
