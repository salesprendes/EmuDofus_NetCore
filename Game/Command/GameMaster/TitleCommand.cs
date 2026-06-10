using Game.Network;

namespace Game.Command
{
    public sealed class TitleCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "titulo", "title"
        };

        public override string[] Aliases => _aliases;
        public override string Description => "Cambia el titulo visible de tu personaje. Uso: %titleId%";
        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            int titleId = 0;
            if (int.TryParse(context.TextCommandArgument.NextWord(), out titleId))
            {
                context.Character.TitleId = titleId;
                context.Character.RefreshOnMap();
            }
            else
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Formato: titulo %titleId%"));
            }
        }
    }
}
