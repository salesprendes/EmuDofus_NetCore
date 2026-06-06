using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class ResetSpellCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "resetspell"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Reinicia los hechizos de un jugador conectado. Uso: %playerName%";

            protected override StaffRole RequiredRole => StaffRole.GameMaster;

            protected override void Process(WorldCommandContext context)
            {
                string characterName = context.TextCommandArgument.NextWord();
                WorldService.Instance.AddMessage(() =>
                {
                    var character = EntityManager.Instance.GetCharacterByName(characterName);
                    if (character == null)
                    {
                        context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Player not found."));
                        return;
                    }

                    character.AddMessage(() =>
                    {
                        character.HardResetSpells();
                    });
                });
            }
        }
    }
}
