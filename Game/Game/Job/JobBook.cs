using Game.Entity;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Job
{
    public sealed class JobBook : MessageDispatcher
    {
        public const int MaxBaseJobs = 3;
        public const int MaxSpecializations = 3;
        public const int MinimumLevelBeforeNextJob = 30;
        public const int SpecializationRequiredParentLevel = 65;

        public int BaseJobCount => m_jobs.Count(IsBaseJob);
        public int SpecializationCount => m_jobs.Count(IsSpecialization);
        private readonly CharacterEntity m_character;
        private List<JobEntry> m_jobs;

        public JobBook(CharacterEntity character)
        {
            m_character = character;
            m_jobs = DeserializeJobs(character.DatabaseRecord.Jobs);

            EnsureBaseJob();
            AddHandler(character.Dispatch);
        }

        public bool TryLearnJob(int jobId, out string reason)
        {
            return TryLearnJob(jobId, 1, out reason);
        }

        public bool TryLearnJob(int jobId, int level, out string reason)
        {
            reason = null;

            if (!CanLearnJob(jobId, level, out reason))
                return false;

            AddJobInternal(jobId, level, ExperienceFloor(level), 0);
            Persist();
            return true;
        }

        public bool CanLearnJob(int jobId, int level, out string reason)
        {
            reason = null;

            var template = JobManager.Instance.GetById(jobId);
            if (template == null || template.Id == JobIdEnum.JOB_NONE || template.Id == JobIdEnum.JOB_BASE)
            {
                reason = "oficio no encontrado";
                return false;
            }

            if (HasJob(jobId))
            {
                reason = "oficio ya aprendido";
                return false;
            }

            if (level < 1 || ExperienceFloor(level) < 0)
            {
                reason = "nivel de oficio no valido";
                return false;
            }

            if (m_jobs.Any(job => job.JobId != (int)JobIdEnum.JOB_BASE && job.Level < MinimumLevelBeforeNextJob))
            {
                reason = "todos los oficios actuales deben ser nivel 30";
                return false;
            }

            if (IsSpecialization(template))
                return CanLearnSpecialization(template, out reason);

            if (BaseJobCount >= MaxBaseJobs)
            {
                reason = "limite de oficios alcanzado";
                return false;
            }

            return true;
        }

        public void LearnJob(int jobId)
        {
            TryLearnJob(jobId, out _);
        }

        public void AddJob(JobIdEnum jobId)
        {
            AddJob((int)jobId);
        }

        public void AddJob(int jobId)
        {
            TryLearnJob(jobId, out _);
        }

        public JobSkill GetSkill(int skillId)
        {
            foreach (var job in m_jobs)
            {
                var template = JobManager.Instance.GetById(job.JobId);
                var skill = template?.GetSkill(m_character, skillId, job.Level);
                if (skill != null)
                    return skill;
            }

            return null;
        }

        public bool HasSkill(int skillId)
        {
            return GetSkill(skillId) != null;
        }

        public bool HasSkill(SkillIdEnum id)
        {
            return HasSkill((int)id);
        }

        public void ToolEquipped(int templatId)
        {
            foreach (var job in m_jobs)
            {
                var template = JobManager.Instance.GetById(job.JobId);
                if (template != null && template.HasTool(templatId))
                {
                    m_character.SafeDispatch(WorldMessage.JOB_TOOL_EQUIPPED(job.JobId.ToString()));
                    return;
                }
            }
        }

        public int GetJobId(SkillIdEnum skill)
        {
            return TryGetJobId(skill, out var jobId) ? jobId : 0;
        }

        public bool TryGetJobId(SkillIdEnum skill, out int jobId)
        {
            foreach (var job in m_jobs)
            {
                var template = JobManager.Instance.GetById(job.JobId);
                if (template != null && template.HasSkill(m_character, skill, job.Level))
                {
                    jobId = job.JobId;
                    return true;
                }
            }

            jobId = 0;
            return false;
        }

        public bool HasJob(int jobId)
        {
            return FindJobIndex(jobId) >= 0;
        }

        public int GetJobLevel(int jobId)
        {
            return TryGetJob(jobId, out var job) ? job.Level : 0;
        }

        public int GetPodsBonus()
        {
            return m_jobs.Sum(job => job.Level * 5 + (job.Level >= 100 ? 1000 : 0));
        }

        public int GetCraftMaxCase(int jobId)
        {
            return TryGetJob(jobId, out var job) ? GetCraftMaxCaseForLevel(job.Level) : 0;
        }

        public int GetCraftSuccessPercent(int jobId, int caseCount)
        {
            return TryGetJob(jobId, out var job) ? GetCraftSuccessPercentForLevel(job.Level, caseCount) : 0;
        }

        public long GetCraftExperience(int jobId, int caseCount)
        {
            return TryGetJob(jobId, out var job) ? GetCraftExperienceForLevel(job.Level, caseCount) : 0;
        }

        public int GetHarvestMinQuantity(int jobId)
        {
            return TryGetJob(jobId, out var job) ? GetHarvestMinQuantityForLevel(job.Level) : 0;
        }

        public int GetHarvestMaxQuantity(int jobId)
        {
            return TryGetJob(jobId, out var job) ? GetHarvestMaxQuantityForLevel(job.Level) : 0;
        }

        public int GetHarvestDuration(int jobId)
        {
            return TryGetJob(jobId, out var job) ? GetHarvestDurationForLevel(job.Level) : 0;
        }

        public void ChangeOptions(int jobId, int optionParams, int minSlots)
        {
            var visibleIndex = 0;

            for (var i = 0; i < m_jobs.Count; i++)
            {
                var job = m_jobs[i];
                if (job.JobId == (int)JobIdEnum.JOB_BASE)
                    continue;

                if (job.JobId == jobId)
                {
                    job.Options = BuildJobOptions(optionParams, minSlots);
                    m_jobs[i] = job;
                    Persist();

                    base.Dispatch(WorldMessage.JOB_OPTIONS(visibleIndex, GetJobOptionParams(job.Options), GetJobMinSlots(job.Options)));
                    return;
                }

                visibleIndex++;
            }
        }

        public void SetJobLevel(int jobId, int level)
        {
            var index = FindJobIndex(jobId);
            if (index < 0)
                return;

            var experience = ExperienceFloor(level);
            if (level < 1 || experience < 0)
                return;

            var job = m_jobs[index];
            job.Level = level;
            job.Experience = experience;
            m_jobs[index] = job;
            Persist();
        }

        public void AddExperience(int jobId, long experience)
        {
            var index = FindJobIndex(jobId);
            if (index < 0)
                return;

            experience = (long)(experience * WorldConfig.RATE_XP);
            if (experience <= 0)
                return;

            var job = m_jobs[index];
            job.Experience += experience;

            var currentLevel = job.Level;
            while (job.Experience > ExperienceFloorNext(job))
                job.Level++;

            m_jobs[index] = job;
            Persist();

            if (job.Level != currentLevel)
            {
                base.Dispatch(WorldMessage.JOB_NEW_LEVEL(job.JobId, job.Level));
                base.Dispatch(WorldMessage.JOB_SKILL(this, job.JobId));
            }

            base.Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_WON_JOB_XP, experience, job.JobId));
            base.Dispatch(WorldMessage.JOB_XP(this, job.JobId));
        }

        public void SerializeAs_SkillListMessage(StringBuilder message)
        {
            var startLength = message.Length;

            foreach (var job in m_jobs)
            {
                if (job.JobId == (int)JobIdEnum.JOB_BASE)
                    continue;

                var template = JobManager.Instance.GetById(job.JobId);
                template?.SerializeAs_SkillListMessage(job.Level, message);
            }

            RemoveTrailingSeparator(message, startLength, '|');
        }

        public bool SerializeAs_SkillListMessage(int jobId, StringBuilder message)
        {
            if (!TryGetJob(jobId, out var job))
                return false;

            var template = JobManager.Instance.GetById(job.JobId);
            if (template == null)
                return false;

            var startLength = message.Length;
            template.SerializeAs_SkillListMessage(job.Level, message);
            RemoveTrailingSeparator(message, startLength, '|');
            return true;
        }

        public void SerializeAs_JobXpMessage(StringBuilder message)
        {
            var startLength = message.Length;

            foreach (var job in m_jobs)
            {
                if (job.JobId == (int)JobIdEnum.JOB_BASE)
                    continue;

                AppendJobXp(message, job);
                message.Append('|');
            }

            RemoveTrailingSeparator(message, startLength, '|');
        }

        public bool SerializeAs_JobXpMessage(int jobId, StringBuilder message)
        {
            if (!TryGetJob(jobId, out var job) || job.JobId == (int)JobIdEnum.JOB_BASE)
                return false;

            AppendJobXp(message, job);
            return true;
        }

        public static int GetCraftMaxCaseForLevel(int jobLevel)
        {
            if (jobLevel < 10) return 2;
            if (jobLevel < 20) return 3;
            if (jobLevel < 40) return 4;
            if (jobLevel < 60) return 5;
            if (jobLevel < 80) return 6;
            if (jobLevel < 100) return 7;
            return 8;
        }

        public static long GetCraftExperienceForLevel(int jobLevel, int caseCount)
        {
            if (jobLevel >= 100)
                return 0;

            switch (caseCount)
            {
                case 1: if (jobLevel < 10) return 1; break;
                case 2: if (jobLevel < 60) return 10; break;
                case 3: if (jobLevel > 9 && jobLevel < 80) return 25; break;
                case 4: if (jobLevel > 19) return 50; break;
                case 5: if (jobLevel > 39) return 100; break;
                case 6: if (jobLevel > 59) return 250; break;
                case 7: if (jobLevel > 79) return 500; break;
                case 8: if (jobLevel > 99) return 1000; break;
            }

            return 0;
        }

        public static int GetCraftSuccessPercentForLevel(int jobLevel, int caseCount)
        {
            var maxCase = GetCraftMaxCaseForLevel(jobLevel);
            return maxCase - caseCount > 2 ? 100 : (jobLevel / 2) + 50;
        }

        public static int GetHarvestMinQuantityForLevel(int jobLevel)
        {
            return 1 + (int)Math.Floor((double)jobLevel / 5) + 6 * (int)Math.Floor((double)jobLevel / 100);
        }

        public static int GetHarvestMaxQuantityForLevel(int jobLevel)
        {
            return GetHarvestMinQuantityForLevel(jobLevel) + 2;
        }

        public static int GetHarvestDurationForLevel(int jobLevel)
        {
            return Math.Max(2000, (int)(1000 * (10 - Math.Round(0.1 * (jobLevel - 1), 1))));
        }

        private bool CanLearnSpecialization(JobTemplate template, out string reason)
        {
            reason = null;

            if (SpecializationCount >= MaxSpecializations)
            {
                reason = "limite de especializaciones alcanzado";
                return false;
            }

            if (!TryGetJob((int)template.ParentJobId, out var parent))
            {
                reason = "falta el oficio base de la especializacion";
                return false;
            }

            if (parent.Level < SpecializationRequiredParentLevel)
            {
                reason = "el oficio base debe ser nivel 65";
                return false;
            }

            return true;
        }

        private void EnsureBaseJob()
        {
            if (HasJob((int)JobIdEnum.JOB_BASE))
                return;

            m_jobs.Insert(0, CreateJob((int)JobIdEnum.JOB_BASE, 1, 0, 0));
        }

        private void AddJobInternal(int jobId, int level, long experience, int options)
        {
            var job = CreateJob(jobId, level, experience, options);
            if (IsSpecialization(job))
                InsertInSlot(job, MaxBaseJobs, MaxSpecializations);
            else
                InsertInSlot(job, 0, MaxBaseJobs);
        }

        private void InsertInSlot(JobEntry job, int startSlot, int slotCount)
        {
            var orderedJobs = OrderedPersistentJobs().ToList();
            var insertIndex = startSlot;
            var endSlot = startSlot + slotCount;

            while (insertIndex < endSlot && orderedJobs[insertIndex].HasValue)
                insertIndex++;

            if (insertIndex >= endSlot)
                return;

            orderedJobs[insertIndex] = job;
            RebuildJobs(orderedJobs);
        }

        private JobEntry?[] OrderedPersistentJobs()
        {
            var slots = new JobEntry? [MaxBaseJobs + MaxSpecializations];
            var baseIndex = 0;
            var specializationIndex = MaxBaseJobs;

            foreach (var job in m_jobs)
            {
                if (job.JobId == (int)JobIdEnum.JOB_BASE)
                    continue;

                if (IsSpecialization(job))
                {
                    if (specializationIndex < slots.Length)
                        slots[specializationIndex++] = job;
                }
                else if (baseIndex < MaxBaseJobs)
                {
                    slots[baseIndex++] = job;
                }
            }

            return slots;
        }

        private void RebuildJobs(IEnumerable<JobEntry?> persistentSlots)
        {
            var baseIndex = FindJobIndex((int)JobIdEnum.JOB_BASE);
            var baseJob = baseIndex >= 0 ? m_jobs[baseIndex] : CreateJob((int)JobIdEnum.JOB_BASE, 1, 0, 0);

            m_jobs = new List<JobEntry> { baseJob };
            m_jobs.AddRange(persistentSlots.Where(job => job.HasValue).Select(job => job.Value));
        }

        private void Persist()
        {
            m_character.DatabaseRecord.Jobs = SerializeJobs();
            m_character.DatabaseRecord.IsDirty = true;
        }

        private string SerializeJobs()
        {
            var slots = OrderedPersistentJobs();
            var parts = new string[MaxBaseJobs + MaxSpecializations];

            for (var i = 0; i < slots.Length; i++)
                parts[i] = slots[i].HasValue ? SerializeJob(slots[i].Value) : string.Empty;

            return string.Join("|", parts);
        }

        private static List<JobEntry> DeserializeJobs(string serialized)
        {
            var jobs = new List<JobEntry>();
            if (string.IsNullOrWhiteSpace(serialized))
                return jobs;

            var slots = serialized.Split('|');
            foreach (var slot in slots.Take(MaxBaseJobs + MaxSpecializations))
            {
                var job = DeserializeJob(slot);
                if (job.HasValue && !jobs.Any(entry => entry.JobId == job.Value.JobId))
                    jobs.Add(job.Value);
            }

            return jobs;
        }

        private static string SerializeJob(JobEntry job)
        {
            return job.JobId + "," + job.Level + "," + job.Experience + "," + job.Options;
        }

        private static JobEntry? DeserializeJob(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var parts = value.Split(',');
            if (!int.TryParse(parts[0], out var jobId) || JobManager.Instance.GetById(jobId) == null)
                return null;

            var level = 1;
            var experience = 0L;
            var options = 0;

            if (parts.Length > 1)
                int.TryParse(parts[1], out level);
            if (parts.Length > 2)
                long.TryParse(parts[2], out experience);
            if (parts.Length > 3)
                int.TryParse(parts[3], out options);

            if (level < 1)
                level = 1;
            if (experience < 0)
                experience = 0;

            return CreateJob(jobId, level, experience, options);
        }

        private static JobEntry CreateJob(int jobId, int level, long experience, int options)
        {
            return new JobEntry { JobId = jobId, Level = level, Experience = experience, Options = options, };
        }

        private bool TryGetJob(int jobId, out JobEntry job)
        {
            var index = FindJobIndex(jobId);
            if (index >= 0)
            {
                job = m_jobs[index];
                return true;
            }

            job = default;
            return false;
        }

        private int FindJobIndex(int jobId)
        {
            return m_jobs.FindIndex(job => job.JobId == jobId);
        }

        private static void AppendJobXp(StringBuilder message, JobEntry job)
        {
            message.Append(job.JobId).Append(';');
            message.Append(job.Level).Append(';');
            message.Append(ExperienceFloorCurrent(job)).Append(';');
            message.Append(job.Experience).Append(';');
            message.Append(ExperienceFloorNext(job));
        }

        private static void RemoveTrailingSeparator(StringBuilder message, int startLength, char separator)
        {
            if (message.Length > startLength && message[message.Length - 1] == separator)
                message.Remove(message.Length - 1, 1);
        }

        private static int BuildJobOptions(int optionParams, int minSlots)
        {
            if (minSlots < 2)
                minSlots = 2;

            return (optionParams & 0xFF) | ((minSlots & 0xFF) << 8);
        }

        private static int GetJobOptionParams(int options)
        {
            return options & 0xFF;
        }

        private static int GetJobMinSlots(int options)
        {
            var minSlots = (options >> 8) & 0xFF;
            return minSlots < 2 ? 2 : minSlots;
        }

        private static bool IsBaseJob(JobEntry job)
        {
            return job.JobId != (int)JobIdEnum.JOB_BASE && !IsSpecialization(job);
        }

        private static bool IsSpecialization(JobEntry job)
        {
            var template = JobManager.Instance.GetById(job.JobId);
            return template != null && IsSpecialization(template);
        }

        private static bool IsSpecialization(JobTemplate template)
        {
            return template.ParentJobId != JobIdEnum.JOB_NONE;
        }

        private static long ExperienceFloor(int level)
        {
            return ExperienceManager.Instance.GetFloor(level, ExperienceTypeEnum.JOB);
        }

        private static long ExperienceFloorCurrent(JobEntry job)
        {
            return ExperienceManager.Instance.GetFloor(job.Level, ExperienceTypeEnum.JOB);
        }

        private static long ExperienceFloorNext(JobEntry job)
        {
            var next = ExperienceManager.Instance.GetFloor(job.Level + 1, ExperienceTypeEnum.JOB);
            return next == -1 ? job.Experience : next;
        }

        private struct JobEntry
        {
            public int JobId;

            public int Level;

            public long Experience;

            public int Options;
        }
    }
}
