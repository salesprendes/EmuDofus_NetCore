using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed class ResetSpellCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "resethechizos", "resetspell" };

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
                    context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Jugador no encontrado."));
                    return;
                }

                character.AddMessage(() => { character.HardResetSpells(); });
            });
        }
    }
}
