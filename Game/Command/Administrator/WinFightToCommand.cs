using Game.Action;
using Game.Network;
using System;
using System.Linq;

namespace Game.Command
{
    public sealed class WinFightToCommand : WorldStaffCommand
    {
        private readonly string[] _aliases =
        {
            "ganarcombatea", "winfightto"
        };

        public override string[] Aliases => _aliases;

        public override string Description => "Hace ganar el combate al equipo del jugador indicado.";

        protected override StaffRole RequiredRole => StaffRole.Administrator;

        protected override void Process(WorldCommandContext context)
        {
            if (!context.Character.HasGameAction(GameActionTypeEnum.FIGHT))
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("No puedes usar este comando fuera de un combate."));
                return;
            }

            var targetName = context.TextCommandArgument.NextWord().Trim();
            var target = context.Character.Fight.Fighters.FirstOrDefault(fighter => fighter.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Nombre de personaje no encontrado."));
                return;
            }

            foreach (var fighter in target.Team.OpponentTeam.AliveFighters)
            {
                fighter.Life = 0;
            }
        }
    }
}
