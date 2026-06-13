using Game.Database.Structure;
using Game.Entity;
using Game.Interactive.Type;
using Game.Job;
using Game.Job.Skill;
using Game.Network;
using Game.Spell;
using Protocolo.Framework.Generic;
using System.Collections.Generic;
using System.Linq;

namespace Game.Exchange
{
    public sealed class CraftPlanExchange : AbstractExchange, IValidableExchange, IRetryableExchange
    {
        private const int LOOP_OK = 1;
        private const int LOOP_INTERUPT = 2;
        private const int LOOP_ERROR = 3;
        private const int LOOP_INVALID = 4;

        // Runa de firma "firmado por": no cuenta como ingrediente; firma el objeto creado.
        private const int SIGNING_ITEM_TEMPLATE = 7508;

        public CharacterEntity Character
        {
            get;
            private set;
        }

        public int JobId
        {
            get;
            private set;
        }

        public CraftSkill Skill
        {
            get;
            private set;
        }

        public int MaxCase
        {
            get;
            set;
        }

        private Dictionary<long, int> m_caseItems;
        private Dictionary<long, int> m_lastCaseItems;
        private Dictionary<int, long> m_templateQuantity;
        private ItemTemplateDAO m_craftItem;
        private int m_loopCount;
        private UpdatableTimer m_loopTimer;
        private CraftPlan m_plan;

        public CraftPlanExchange(CharacterEntity character, CraftPlan plan, JobSkill skill, ExchangeTypeEnum type = ExchangeTypeEnum.EXCHANGE_CRAFTPLAN)
    : base(type)
        {
            m_caseItems = new Dictionary<long, int>();
            m_templateQuantity = new Dictionary<int, long>();
            m_plan = plan;
            Character = character;
            Skill = (CraftSkill)skill;
            JobId = Character.CharacterJobs.GetJobId(skill.Id);
            MaxCase = Character.CharacterJobs.GetCraftMaxCase(JobId);
        }

        protected override string SerializeAs_ExchangeCreate()
        {
            return MaxCase + ";" + (int)Skill.Id;
        }

        public override void Leave(bool success = false)
        {
            CancelRetry();

            m_plan.StopCraft();

            base.Leave(success);
        }

        public override int AddItem(AbstractEntity entity, long guid, int quantity, long price = -1)
        {
            var item = Character.Inventory.GetItem(guid);
            if (item == null)
                return 0;

            if (quantity > item.Quantity)
                quantity = item.Quantity;

            if (item != null && item.Slot == ItemSlotEnum.SLOT_INVENTORY)
            {
                var alreadyExchangedQuantity = GetQuantity(guid);
                if (alreadyExchangedQuantity > 0)
                {
                    var realQuantity = item.Quantity - alreadyExchangedQuantity;
                    if (quantity > realQuantity)
                        quantity = realQuantity;
                }

                if (!m_templateQuantity.ContainsKey(item.TemplateId))
                    m_templateQuantity.Add(item.TemplateId, 0);
                m_templateQuantity[item.TemplateId] += quantity;
                m_caseItems[guid] += quantity;

                Character.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, item.Id.ToString() + '|' + m_caseItems[guid]));

                CheckCraftable();

                return quantity;
            }

            return 0;
        }

        public override int RemoveItem(AbstractEntity entity, long guid, int quantity)
        {
            if (m_caseItems.ContainsKey(guid))
            {
                var item = entity.Inventory.Items.Find(entry => entry.Id == guid);
                if (quantity >= m_caseItems[guid])
                {
                    quantity = m_caseItems[guid];
                    m_caseItems.Remove(guid);
                }
                else
                {
                    m_caseItems[guid] -= quantity;
                }
                m_templateQuantity[item.TemplateId] -= quantity;
                if (m_templateQuantity[item.TemplateId] == 0)
                    m_templateQuantity.Remove(item.TemplateId);

                CheckCraftable();

                var exists = m_caseItems.ContainsKey(guid);
                Character.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_REMOVE, item.Id.ToString()));
                if (exists)
                    Character.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, item.Id.ToString() + '|' + m_caseItems[guid]));

                return quantity;
            }
            return 0;
        }

        private int GetQuantity(long guid)
        {
            if (m_caseItems.ContainsKey(guid))
                return m_caseItems[guid];
            else
                m_caseItems.Add(guid, 0);
            return 0;
        }

        private void CheckCraftable()
        {
            // La firma (7508) no es un ingrediente de la receta: se excluye del emparejado.
            var recipe = m_templateQuantity.ContainsKey(SIGNING_ITEM_TEMPLATE)
                ? m_templateQuantity.Where(kv => kv.Key != SIGNING_ITEM_TEMPLATE).ToDictionary(kv => kv.Key, kv => kv.Value)
                : m_templateQuantity;

            m_craftItem = (recipe.Count > 0) ? Skill.Craftables.Find(entry => entry.MatchCraft(recipe)) : null;
        }

        public bool Validate(AbstractEntity entity)
        {
            Character.CachedBuffer = true;

            // La firma se consume con los ingredientes pero no cuenta como tal.
            var signed = m_templateQuantity.ContainsKey(SIGNING_ITEM_TEMPLATE);
            var recipeCaseCount = m_caseItems.Keys.Count(guid =>
                Character.Inventory.GetItem(guid)?.TemplateId != SIGNING_ITEM_TEMPLATE);

            foreach (var item in m_caseItems)
            {
                var templateId = Character.Inventory.RemoveItem(item.Key, item.Value).TemplateId;
                m_templateQuantity[templateId] -= item.Value;
            }

            if (m_craftItem != null)
            {
                var chance = Character.CharacterJobs.GetCraftSuccessPercent(JobId, recipeCaseCount);
                var success = Util.Next(0, 100) < chance;

                if (success)
                {
                    ItemDAO item = m_craftItem.Create(Character.Id, (int)Character.Type);

                    // Firma de artesano: "Fabricado por <nombre>".
                    if (signed)
                    {
                        item.Statistics.AddEffect(EffectEnum.MadeBy, 0, 0, 0, Character.Name);
                        item.SaveStats();
                        Character.Inventory.AddItem(item, merge: false);
                    }
                    else
                    {
                        Character.Inventory.AddItem(item);
                        item = Character.Inventory.Items.Find(entry => entry.TemplateId == m_craftItem.Id);
                    }

                    Character.Dispatch(WorldMessage.EXCHANGE_DISTANT_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, item.Id + "|1|" + m_craftItem.Id + "|" + item.StringEffects));
                    Character.Dispatch(WorldMessage.CRAFT_TEMPLATE_CREATED(item.TemplateId));
                    if (m_loopTimer == null)
                        Character.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_SUCCESS(Character.Id, item.TemplateId));
                }
                else
                {
                    Character.Dispatch(WorldMessage.CRAFT_TEMPLATE_FAILED(m_craftItem.Id));
                    if (m_loopTimer == null)
                        Character.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_FAILED(Character.Id, m_craftItem.Id));
                }

                Character.CharacterJobs.AddExperience(JobId, Character.CharacterJobs.GetCraftExperience(JobId, m_templateQuantity.Count));
            }
            else
            {
                Character.Dispatch(WorldMessage.CRAFT_NO_RESULT());
                if (m_loopTimer == null)
                    Character.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_NOTHING(Character.Id));
            }

            Character.CachedBuffer = false;

            m_lastCaseItems = m_caseItems;
            m_caseItems = new Dictionary<long, int>();

            return false;
        }

        public void Retry(int count)
        {
            if (m_loopTimer != null)
                return;

            m_loopCount = count;
            m_loopTimer = base.AddTimer(1100, Loop);
        }

        public void CancelRetry()
        {
            if (m_loopTimer == null)
                return;

            EndLoop(LOOP_INTERUPT);
        }

        private void Loop()
        {
            Character.CachedBuffer = true;

            Character.Dispatch(WorldMessage.CRAFT_LOOP_COUNT(m_loopCount - 1));

            foreach (var ingredient in m_lastCaseItems)
            {
                var item = Character.Inventory.GetItem(ingredient.Key);
                if (item == null || item.Quantity < ingredient.Value)
                {
                    EndLoop(LOOP_ERROR);
                    Character.CachedBuffer = false;
                    return;
                }
                AddItem(Character, ingredient.Key, ingredient.Value);
            }

            Validate(null);

            m_loopCount--;

            if (m_loopCount == 0)
            {
                EndLoop(LOOP_OK);

                m_caseItems = new Dictionary<long, int>();
            }

            Character.CachedBuffer = false;
        }

        private void EndLoop(int reason)
        {
            Character.Dispatch(WorldMessage.CRAFT_LOOP_END(reason));

            base.RemoveTimer(m_loopTimer);
            m_loopTimer = null;
        }
    }
}


