using Game.Job;
using Game.Manager;
using Game.Network;
using System;

namespace Game.Command
{
    public sealed class LearnJobCommand : WorldStaffCommand
    {
        private readonly string[] _aliases = { "oficio", "job" };

        public override string[] Aliases => _aliases;
        public override string Description => "Hace que un personaje aprenda un oficio. Uso: oficio %jobId|nombre% [%nivel%] [%nombreJugador%]";
        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            var jobArg = context.TextCommandArgument.NextWord();
            if (!TryResolveJobId(jobArg, out var jobId))
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Formato: oficio %jobId|nombre% [%nivel%] [%nombreJugador%]"));
                return;
            }

            var template = JobManager.Instance.GetById(jobId);
            if (template == null || template.Id == JobIdEnum.JOB_NONE || template.Id == JobIdEnum.JOB_BASE)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Oficio no encontrado."));
                return;
            }

            var level = 1;
            var levelOrTarget = context.TextCommandArgument.NextWord();
            var targetName = string.Empty;

            if (!string.IsNullOrEmpty(levelOrTarget))
            {
                if (int.TryParse(levelOrTarget, out level))
                    targetName = context.TextCommandArgument.NextWord();
                else
                    targetName = levelOrTarget;
            }

            var experienceFloor = ExperienceManager.Instance.GetFloor(level, ExperienceTypeEnum.JOB);
            if (level < 1 || experienceFloor < 0)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Nivel de oficio no valido."));
                return;
            }

            var target = string.IsNullOrEmpty(targetName) ? context.Character : EntityManager.Instance.GetCharacterByName(targetName);

            if (target == null)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE($"Personaje {targetName} no encontrado o no esta conectado."));
                return;
            }

            if (!target.CharacterJobs.TryLearnJob(jobId, level, out var reason))
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE($"No se pudo aprender el oficio: {reason}."));
                return;
            }

            target.CachedBuffer = true;
            target.Dispatch(WorldMessage.JOB_SKILL(target.CharacterJobs));
            target.Dispatch(WorldMessage.JOB_XP(target.CharacterJobs));
            target.Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_JOB_LEARNT, jobId));
            target.CachedBuffer = false;

            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE($"{target.Name} aprendio el oficio {template.Id} al nivel {level}."));
        }

        private static bool TryResolveJobId(string value, out int jobId)
        {
            jobId = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (int.TryParse(value, out jobId))
                return true;

            var enumName = value.StartsWith("JOB_", StringComparison.OrdinalIgnoreCase) ? value : "JOB_" + value;

            if (!Enum.TryParse(enumName, true, out JobIdEnum resolved))
                return false;

            jobId = (int)resolved;
            return true;
        }
    }
}
