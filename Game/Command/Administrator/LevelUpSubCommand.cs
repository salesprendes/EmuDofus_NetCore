using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class LevelUpSubCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "level"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Sube tu personaje hasta el nivel indicado. Uso: %level%";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                int level;
                if (int.TryParse(context.TextCommandArgument.NextWord(), out level))
                {
                    if (level > context.Character.Level)
                    {
                        while (level > context.Character.Level)
                        {
                            context.Character.LevelUp();
                        }

                        context.Character.Dispatch(WorldMessage.CHARACTER_NEW_LEVEL(context.Character.Level));
                        context.Character.Dispatch(WorldMessage.SPELLS_LIST(context.Character.SpellBook));
                        context.Character.Dispatch(WorldMessage.ACCOUNT_STATS(context.Character));
                        context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("You are now level " + level));
                    }
                    else
                    {
                        context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("New level should be higher than yours"));
                    }
                }
                else
                {
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Command format : character levelup %level%"));
                }
            }
        }
    }
}
