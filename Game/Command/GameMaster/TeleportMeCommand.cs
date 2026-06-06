using Game.Action;
using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class TeleportMeCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "teleme"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Trae a un jugador hasta tu posicion. Uso: %playerName%";

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

                    var mapId = context.Character.MapId;
                    var cellId = context.Character.CellId;

                    character.AddMessage(() =>
                    {
                        if (character.HasGameAction(GameActionTypeEnum.FIGHT))
                        {
                            context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Unable to teleport remote player: player is in a fight."));
                            return;
                        }

                        character.CloseCurrentInteraction();
                        character.Teleport(mapId, cellId);
                        context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Player teleported successfully."));
                    });
                });
            }
        }
    }
}
