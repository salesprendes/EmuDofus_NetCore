using Protocolo.Framework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// 
        /// </summary>
        public sealed class SpawnMonsterCommand : SubCommand<WorldCommandContext>
        {
            private readonly string[] _aliases = 
            {
                "spawn"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Spawn a monsters group.";

            protected override bool CanExecute(WorldCommandContext context)
            {
                return true;
            }

            protected override void Process(WorldCommandContext context)
            {
                //context.Character.Map.SpawnMonsters(context.Character.CellId);
            }
        }
    }
}

