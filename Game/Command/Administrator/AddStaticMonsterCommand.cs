using Game.Database.Structure;
using Game.Fight;
using Game.Network;

namespace Game.Command
{
    public sealed partial class WorldCommand
    {
        public sealed class AddStaticMonsterCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "addstaticmonster"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Guarda un punto fijo de aparicion de monstruos en el mapa.";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                var gradeId = int.Parse(context.TextCommandArgument.NextWord());
                new MonsterSpawnDAO()
                {
                    ZoneType = (int)ZoneTypeEnum.TYPE_MAP,
                    ZoneId = (int)context.Character.MapId,
                    GradeId = gradeId,
                    Probability = 1,
                }.Insert();

                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("MonsterSpawn added."));
            }
        }
    }
}
