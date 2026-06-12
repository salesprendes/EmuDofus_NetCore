using Protocolo.Framework.Generic;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Action;
using Game.Entity;
using Game.Job;
using Game.Job.Skill;
using Game.Map;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Interactive.Type
{
    public sealed class HarvestableResource : InteractiveObject
    {
        public const int FRAME_FARMING = 3;

        public const int FRAME_CUT = 4;

        public const int FRAME_GROW = 5;

        public int GeneratedTemplateId
        {
            get;
            private set;
        }

        public int MinRespawnTime
        {
            get;
            private set;
        }

        public int MaxRespawnTime
        {
            get;
            private set;
        }

        public int Experience
        {
            get;
            private set;
        }

        public ItemTemplateDAO GeneratedTemplate
        {
            get
            {
                if (m_generatedTemplate == null)
                    m_generatedTemplate = ItemTemplateRepository.Instance.GetById(GeneratedTemplateId);
                return m_generatedTemplate;
            }
        }

        private UpdatableTimer m_harvestTimer;

        private CharacterEntity m_currentHarvester;

        private int m_currentJobId;

        private ItemTemplateDAO m_generatedTemplate;

        private int m_quantityFarmed;

        public HarvestableResource(MapInstance map, int cellId, int generatedTemplateId, int minRespawnTime, int maxRespawnTime, int experience, bool walkThrough = false)
    : base(map, cellId, walkThrough)
        {
            GeneratedTemplateId = generatedTemplateId;
            MinRespawnTime = minRespawnTime;
            MaxRespawnTime = maxRespawnTime;
            Experience = experience;
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            switch (skill.Id)
            {
                case SkillIdEnum.SKILL_COUPER_BAMBOU:
                case SkillIdEnum.SKILL_COUPER_BAMBOUSACRE:
                case SkillIdEnum.SKILL_COUPER_BAMBOUSOMBRE:
                case SkillIdEnum.SKILL_COUPER_BOMBU:
                case SkillIdEnum.SKILL_COUPER_CHARME:
                case SkillIdEnum.SKILL_COUPER_CHATAIGNER:
                case SkillIdEnum.SKILL_COUPER_CHENE:
                case SkillIdEnum.SKILL_COUPER_EBENE:
                case SkillIdEnum.SKILL_COUPER_ERABLE:
                case SkillIdEnum.SKILL_COUPER_FRENE:
                case SkillIdEnum.SKILL_COUPER_IF:
                case SkillIdEnum.SKILL_COUPER_KALIPTUS:
                case SkillIdEnum.SKILL_COUPER_MERISIER:
                case SkillIdEnum.SKILL_COUPER_NOYER:
                case SkillIdEnum.SKILL_COUPER_OLIVIOLET:
                case SkillIdEnum.SKILL_COUPER_ORME:
                case SkillIdEnum.SKILL_PECHER_GOUJON:
                case SkillIdEnum.SKILL_PECHER_TRUITE:
                case SkillIdEnum.SKILL_PECHER_POISSONCHATON:
                case SkillIdEnum.SKILL_PECHER_BROCHET:
                case SkillIdEnum.SKILL_PECHER_GREUVETTE:
                case SkillIdEnum.SKILL_PECHER_CRABESOURIMI:
                case SkillIdEnum.SKILL_PECHER_POISSONPANE:
                case SkillIdEnum.SKILL_PECHER_SARDINEBRILLANTE:
                case SkillIdEnum.SKILL_PECHER_PICHONEUDCOMPET:
                case SkillIdEnum.SKILL_PECHER_KRALAMOURE:
                case SkillIdEnum.SKILL_PECHER_SARDINEBRILLANTE_1:
                case SkillIdEnum.SKILL_FAUCHER_BLE:
                case SkillIdEnum.SKILL_FAUCHER_HOUBLON:
                case SkillIdEnum.SKILL_FAUCHER_LIN:
                case SkillIdEnum.SKILL_FAUCHER_SEIGLE:
                case SkillIdEnum.SKILL_FAUCHER_ORGE:
                case SkillIdEnum.SKILL_FAUCHER_CHANVRE:
                case SkillIdEnum.SKILL_FAUCHER_AVOINE:
                case SkillIdEnum.SKILL_FAUCHER_MALT:
                case SkillIdEnum.SKILL_FAUCHER_RIZ:
                case SkillIdEnum.SKILL_CUEILLIR_LIN:
                case SkillIdEnum.SKILL_CUEILLIR_CHANVRE:
                case SkillIdEnum.SKILL_CUEILLIR_TREFLE:
                case SkillIdEnum.SKILL_CUEILLIR_MENTHE:
                case SkillIdEnum.SKILL_CUEILLIR_ORCHIDEE:
                case SkillIdEnum.SKILL_CUEILLIR_EDELWEISS:
                case SkillIdEnum.SKILL_CUEILLIR_PANDOUILLE:
                case SkillIdEnum.SKILL_COLLECTER_FER:
                case SkillIdEnum.SKILL_COLLECTER_CUIVRE:
                case SkillIdEnum.SKILL_COLLECTER_BRONZE:
                case SkillIdEnum.SKILL_COLLECTER_KOBALTE:
                case SkillIdEnum.SKILL_COLLECTER_ARGENT:
                case SkillIdEnum.SKILL_COLLECTER_OR:
                case SkillIdEnum.SKILL_COLLECTER_BAUXITE:
                case SkillIdEnum.SKILL_COLLECTER_ETAIN:
                case SkillIdEnum.SKILL_COLLECTER_MANGANESE:
                case SkillIdEnum.SKILL_COLLECTER_DOLOMITE:
                case SkillIdEnum.SKILL_COLLECTER_SILICATE:
                    Harvest(character, skill.Id);
                    break;
            }
        }

        private void Harvest(CharacterEntity character, SkillIdEnum skill)
        {
            if (!character.CanGameAction(GameActionTypeEnum.SKILL_HARVEST))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_YOU_ARE_AWAY));
                return;
            }

            if (!IsActive)
                return;

            m_currentJobId = character.CharacterJobs.GetJobId(skill);
            if (m_currentJobId == 0)
                return;

            var duration = character.CharacterJobs.GetHarvestDuration(m_currentJobId);
            m_quantityFarmed = Util.Next(character.CharacterJobs.GetHarvestMinQuantity(m_currentJobId), character.CharacterJobs.GetHarvestMaxQuantity(m_currentJobId));

            character.HarvestStart(this, duration);
            m_currentHarvester = character;

            Deactivate();

            m_harvestTimer = base.AddTimer(duration, StopHarvest, true);
        }

        public void AbortHarvest()
        {

            if (m_currentHarvester == null)
                return;

            Activate();

            m_currentHarvester = null;
            m_currentJobId = 0;
            m_quantityFarmed = 0;

            base.RemoveTimer(m_harvestTimer);
            m_harvestTimer = null;
        }

        public void StopHarvest()
        {


            var harvester = m_currentHarvester;
            var jobId = m_currentJobId;
            var quantity = m_quantityFarmed;

            m_currentHarvester = null;
            m_currentJobId = 0;
            m_quantityFarmed = 0;
            m_harvestTimer = null;

            if (harvester == null)
                return;

            harvester.StopAction(GameActionTypeEnum.SKILL_HARVEST);

            var experienceWin = quantity * Experience;

            harvester.CachedBuffer = true;
            harvester.Inventory.AddItem(GeneratedTemplate.Create(harvester.Id, (int)harvester.Type, quantity));
            harvester.CharacterJobs.AddExperience(jobId, experienceWin);
            harvester.Dispatch(WorldMessage.INTERACTIVE_FARMED_QUANTITY(harvester.Id, quantity));
            harvester.CachedBuffer = false;

            base.UpdateFrame(FRAME_FARMING, FRAME_CUT);
            base.AddTimer(Util.Next(MinRespawnTime, MaxRespawnTime), Respawn, true);
        }

        private void Respawn()
        {
            base.UpdateFrame(FRAME_GROW, FRAME_NORMAL, true);
        }
    }
}


