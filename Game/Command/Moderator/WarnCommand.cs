using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed class WarnCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "avisar", "warn" };

        public override string[] Aliases => _aliases;

        public override string Description => "Avisa a un jugador. Uso: %playerName% %motivo%";

        protected override StaffRole RequiredRole => StaffRole.Moderator;

        protected override void Process(WorldCommandContext context)
        {
            var characterName = context.TextCommandArgument.NextWord();
            var reason = context.TextCommandArgument.NextWord();

            WorldService.Instance.AddMessage(() =>
            {
                var character = EntityManager.Instance.GetCharacterByName(characterName);
                if (character == null)
                {
                    context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Jugador no encontrado."));
                    return;
                }

                character.SafeDispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_BASIC_WARNING_BEFORE_SANCTION, reason));
                context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Jugador avisado correctamente."));
            });
        }
    }
}
