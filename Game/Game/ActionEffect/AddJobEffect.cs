using Game.Manager;
using Game.Network;
using System.Collections.Generic;

namespace Game.ActionEffect
{
    public sealed class AddJobEffect : AbstractActionEffect<AddJobEffect>
    {
        public override bool ProcessItem(Entity.CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, new Dictionary<string, string>() { { "jobId", effect.Value1.ToString() } });
        }

        public override bool Process(Entity.CharacterEntity character, Dictionary<string, string> parameters)
        {
            var jobId = int.Parse(parameters["jobId"]);

            var jobTemplate = JobManager.Instance.GetById(jobId);
            if (jobTemplate == null)
            {
                return false;
            }

            if (!character.CharacterJobs.TryLearnJob(jobId, out var reason))
            {
                if (reason == "oficio ya aprendido")
                    character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_ALREADY_JOB));
                else if (reason == "limite de oficios alcanzado" || reason == "limite de especializaciones alcanzado")
                    character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_TOO_MUCH_JOB));
                else
                    character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_UNABLE_LEARN_JOB));
                return false;
            }

            character.CachedBuffer = true;
            character.Dispatch(WorldMessage.JOB_SKILL(character.CharacterJobs));
            character.Dispatch(WorldMessage.JOB_XP(character.CharacterJobs));
            character.Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_JOB_LEARNT, jobId));
            character.CachedBuffer = false;

            return true;
        }
    }
}

