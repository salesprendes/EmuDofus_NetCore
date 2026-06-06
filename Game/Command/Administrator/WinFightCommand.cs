using Game.Action;
using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class WinFightCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "winfight"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Hace ganar el combate actual a tu equipo.";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                if (!context.Character.HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Unable to execute this command out of fight."));
                    return;
                }

                foreach (var fighter in context.Character.Team.OpponentTeam.AliveFighters)
                {
                    fighter.Life = 0;
                }
            }
        }
    }
}
