using Game.Database.Repository;
using Game.Database.Structure;
using Protocolo.Framework.Command;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Command
{
    public sealed class MonsterCommand : Command<WorldCommandContext>
    {
        private static readonly string[] m_aliases = { "monster", "m" };

        public override string[] Aliases => m_aliases;

        public override string Description => "Monsters management commands";

        protected override void Process(WorldCommandContext context)
        {
            base.Process(context);
        }
    }

    public sealed class SpawnMonsterCommand : Command<WorldCommandContext>
    {
        private static readonly string[] m_aliases = { "spawn" };

        public override string[] Aliases => m_aliases;

        public override string Description => "Spawn a monster group: .spawn <monsterId> [cantidad]";

        protected override bool CanExecute(WorldCommandContext context)
        {
            return true;
        }

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
