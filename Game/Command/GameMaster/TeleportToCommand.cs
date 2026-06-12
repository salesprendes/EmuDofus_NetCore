using Game.Action;
using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed class TeleportToCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "ir", "teleto" };

        public override string[] Aliases => _aliases;

        public override string Description => "Te lleva hasta la posicion de un jugador. Uso: %playerName%";

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

                var mapId = character.MapId;
                var cellId = character.CellId;

                context.Character.AddMessage(() =>
                {
                    if (context.Character.HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("No puedes teletransportarte mientras estas en combate."));
                        return;
                    }

                    context.Character.CloseCurrentInteraction();
                    context.Character.Teleport(mapId, cellId);
                });
            });
        }
    }
}
