using Game.Entity;
using Game.Manager;
using Game.Network;
using System;

namespace Game.Command
{
    public sealed class LevelUpCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "nivel", "level" };
        public override string[] Aliases => _aliases;
        public override string Description => "Sube un personaje hasta el nivel indicado. Uso: nivel %nivel% [%nombre%]";
        protected override StaffRole RequiredRole => StaffRole.Administrator;

        protected override void Process(WorldCommandContext context)
        {
            int level;
            if (!int.TryParse(context.TextCommandArgument.NextWord(), out level))
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Formato: nivel %nivel% [%nombre%]"));
                return;
            }

            string targetName = context.TextCommandArgument.NextWord();
            CharacterEntity target = string.IsNullOrEmpty(targetName) ? context.Character : EntityManager.Instance.GetCharacterByName(targetName);

            if (target == null)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Personaje '" + targetName + "' no encontrado o no está conectado."));
                return;
            }

            if (level <= target.Level)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("El nivel indicado debe ser mayor que el nivel actual del personaje."));
                return;
            }

            long xpFloor = ExperienceManager.Instance.GetFloor(level, ExperienceTypeEnum.CHARACTER);
            if (xpFloor < 0)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Nivel no válido."));
                return;
            }

            target.AddExperience(Math.Max(0, xpFloor - target.Experience + 1));
            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE(target.Name + " ahora es nivel " + level));
        }
    }
}
