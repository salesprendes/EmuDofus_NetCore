using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class KickCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "kick"
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
                        context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Player not found."));
                        return;
                    }

                    if (character.Account.Power >= context.Character.Account.Power)
                    {
                        context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("This player is a god, god cannot be kicked. In addition, he will be noticed."));
                        return;
                    }

                    character.SafeKick(context.Character.Name, reason);
                    context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Player kicked successfully."));
                });
            }
        }
    }
}
