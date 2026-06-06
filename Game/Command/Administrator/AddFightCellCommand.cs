using Game.Database.Repository;
using Game.Network;

namespace Game.Command
{
    public sealed partial class WorldCommand
    {
        public sealed class AddFightCellCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "addfightcell"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Marca la celda actual como posicion inicial de combate.";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                var team = int.Parse(context.TextCommandArgument.NextWord());
                var mapTemplate = MapTemplateRepository.Instance.GetById(context.Character.MapId);
                if(team == 0)
                    mapTemplate.FightTeam0Cells.Add(context.Character.CellId);
                else if(team == 1)
                    mapTemplate.FightTeam1Cells.Add(context.Character.CellId);

                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("FightCell added."));
            }
        }
    }
}
