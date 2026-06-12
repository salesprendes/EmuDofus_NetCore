using System.Linq;
using Protocolo.Framework.Generic;
using Game.Database.Repository;

namespace Game.Manager
{
    public enum ExperienceTypeEnum
    {
        CHARACTER,
        JOB,
        MOUNT,
        PVP,
        LIVING,
        GUILD,
    }

    public sealed class ExperienceManager : Singleton<ExperienceManager>
    {
        public int GetLevel(ExperienceTypeEnum type, long experience)
        {
            int x = 1;
            while (GetFloor(x, type) < experience)
            {
                x++;
            }
            return x;
        }

        public long GetFlootCurrent(ExperienceTypeEnum type, long experience)
    => GetFloor(GetLevel(type, experience), type);

        public long GetFloorNext(ExperienceTypeEnum type, long experience)
    => GetFloor(GetLevel(type, experience) + 1, type);

        public long GetFloor(int level, ExperienceTypeEnum type)
        {
            var template = ExperienceTemplateRepository.Instance.GetByLevel(level);
            if (template == null)
                return -1;
            switch (type)
            {
                case ExperienceTypeEnum.CHARACTER:
                    return template.Character;
                case ExperienceTypeEnum.PVP:
                    return template.Pvp;
                case ExperienceTypeEnum.JOB:
                    return template.Job;
                case ExperienceTypeEnum.MOUNT:
                    return template.Mount;
                case ExperienceTypeEnum.GUILD:
                    return template.Guild;
                case ExperienceTypeEnum.LIVING:
                    return template.Living;
                default:
                    return -1;
            }
        }
    }
}

