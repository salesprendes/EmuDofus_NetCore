using Game.Database.Structure;
using Game.Fight;
using Game.Network;

namespace Game.Command
{
    public sealed class AddFightActionCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "accioncombate", "addfightaction" };

        public override string[] Aliases => _aliases;
        public override string Description => "Anade una accion que se ejecuta al terminar un combate en el mapa.";
        protected override StaffRole RequiredRole => StaffRole.Administrator;

        protected override void Process(WorldCommandContext context)
        {
            var nextMap = int.Parse(context.TextCommandArgument.NextWord());
            var nextCell = int.Parse(context.TextCommandArgument.NextWord());

            new FightActionDAO()
            {
                ZoneType = (int)ZoneTypeEnum.TYPE_MAP,
                ZoneId = context.Character.MapId,
                FightType = (int)FightTypeEnum.TYPE_PVM,
                FightState = (int)FightStateEnum.STATE_ENDED,
                Conditions = "",
                Actions = "2005:mapId=" + nextMap + ",cellId=" + nextCell
            }.Insert();

            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Accion de combate anadida."));
        }
    }
}
