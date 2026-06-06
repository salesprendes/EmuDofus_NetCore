using Game.Database.Repository;
using Game.Database.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Command
{
    public sealed class SpawnMonsterCommand : WorldStaffCommand
    {
        private static readonly string[] m_aliases = { "spawn" };

        public override string[] Aliases => m_aliases;

        public override string Description => "Invoca un grupo de monstruos en tu mapa. Uso: %monsterId% [%cantidad%]";

        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            if (!int.TryParse(context.TextCommandArgument.NextWord(), out var monsterId))
                return;

            var monster = MonsterRepository.Instance.GetById(monsterId);
            if (monster == null || !monster.Grades.Any())
                return;

            List<MonsterGradeDAO> grades = monster.Grades.ToList();
            MonsterGradeDAO grade = grades[Util.Next(0, grades.Count)];

            string countStr = context.TextCommandArgument.NextWord();
            int count = string.IsNullOrEmpty(countStr) ? Util.Next(1, 7) : Math.Max(1, int.Parse(countStr));

            List<MonsterSpawnDAO> spawns = Enumerable.Range(0, count).Select(_ => new MonsterSpawnDAO { GradeId = (int)grade.Id, Probability = 1.0 }).ToList();
            context.Character.Map.SpawnMonsters(spawns);
        }
    }
}
