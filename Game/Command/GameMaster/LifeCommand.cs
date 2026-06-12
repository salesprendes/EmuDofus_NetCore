using Game.Network;

namespace Game.Command
{
    public sealed class LifeCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "vida", "life" };

        public override string[] Aliases => _aliases;

        public override string Description => "Restaura toda la vida de tu personaje.";

        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            context.Character.Life = context.Character.MaxLife;
            context.Character.Dispatch(WorldMessage.ACCOUNT_STATS(context.Character));
        }
    }
}
