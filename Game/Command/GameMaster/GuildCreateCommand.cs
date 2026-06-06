using Game.Action;
using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class GuildCreateCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "guild"
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
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Unable to start a guild creation in your actual state."));
                }
            }
        }
    }
}
