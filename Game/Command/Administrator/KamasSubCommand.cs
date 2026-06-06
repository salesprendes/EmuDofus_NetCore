using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class KamasSubCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "kamas"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Anade kamas a tu inventario. Uso: %kamas%";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                long kamas;
                if (long.TryParse(context.TextCommandArgument.NextWord(), out kamas))
                {
                    context.Character.Inventory.AddKamas(kamas);
                }
                else
                {
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Command format : character kamas %kamas%"));
                }
            }
        }
    }
}
