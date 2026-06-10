using Game.Action;
using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed class TeleportCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "tele", "teleport", "go"
        };

        public override string[] Aliases => _aliases;
        public override string Description => "Te teletransporta a un mapa y celda concretos. Uso: %mapId% %cellId%";
        protected override StaffRole RequiredRole => StaffRole.Moderator;

        protected override void Process(WorldCommandContext context)
        {
            if (!int.TryParse(context.TextCommandArgument.NextWord(), out var mapId))
            {
                SendFormat(context);
                return;
            }

            var map = MapManager.Instance.GetById(mapId);
            if (map == null)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Mapa no encontrado."));
                return;
            }

            int.TryParse(context.TextCommandArgument.NextWord(), out var cellId);

            if (map.GetCell(cellId)?.Walkable != true)
            {
                cellId = map.RandomFreeCell();
                if (cellId == -1)
                {
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("No hay celdas libres en ese mapa."));
                    return;
                }
            }

            if (!context.Character.CanGameAction(GameActionTypeEnum.MAP_TELEPORT))
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("No puedes teletransportarte en tu estado actual."));
                return;
            }

            context.Character.Teleport(mapId, cellId);
        }

        private static void SendFormat(WorldCommandContext context)
        {
            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Formato: tele %mapId% %cellId%"));
        }
    }
}
