using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed class KickCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "expulsar", "kick"
        };

        public override string[] Aliases => _aliases;

        public override string Description => "Expulsa a un jugador conectado. Uso: %playerName% %motivo%";

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

                if (character.Account.Power >= context.Character.Account.Power)
                {
                    context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("No puedes expulsar a un jugador con un rango igual o superior. Ademas, se le notificara el intento."));
                    return;
                }

                character.SafeKick(context.Character.Name, reason);
                context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Jugador expulsado correctamente."));
            });
        }
    }
}
