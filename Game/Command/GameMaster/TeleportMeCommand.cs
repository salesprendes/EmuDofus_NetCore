using Game.Action;
using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed class TeleportMeCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "traer", "teleme"
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
                    context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Jugador no encontrado."));
                    return;
                }

                var mapId = context.Character.MapId;
                var cellId = context.Character.CellId;

                character.AddMessage(() =>
                {
                    if (character.HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("No puedes traer a ese jugador porque esta en combate."));
                        return;
                    }

                    character.CloseCurrentInteraction();
                    character.Teleport(mapId, cellId);
                    context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Jugador teletransportado correctamente."));
                });
            });
        }
    }
}
