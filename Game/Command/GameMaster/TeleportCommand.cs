using Game.Action;
using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed class TeleportCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "tele", "teleport"
        };

        public override string[] Aliases => _aliases;
        public override string Description => "Te teletransporta a un mapa y celda concretos. Uso: %mapId% %cellId%";
        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            if (!int.TryParse(context.TextCommandArgument.NextWord(), out var mapId) || !int.TryParse(context.TextCommandArgument.NextWord(), out var cellId))
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

            var cell = map.GetCell(cellId);
            if (cell == null || !cell.Walkable)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("La celda no existe o no es caminable."));
                return;
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
