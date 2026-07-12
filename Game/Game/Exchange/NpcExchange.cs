using Game.Database.Structure;
using Game.Action;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Exchange
{
    public sealed class NpcExchange : AbstractEntityExchange
    {
        public CharacterEntity Character
        {
            get;
            private set;
        }

        public NonPlayerCharacterEntity Npc
        {
            get;
            private set;
        }

        private Dictionary<int, long> m_templateQuantity;
        private RewardEntry m_reward;

        public NpcExchange(CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(ExchangeTypeEnum.EXCHANGE_NPC, character, npc)
        {
            m_templateQuantity = new Dictionary<int, long>();

            Character = character;
            Npc = npc;
        }

        public override bool Validate(AbstractEntity entity)
        {
            if (m_reward != null)
            {
                Character.CachedBuffer = true;
                Character.Inventory.SubKamas(m_exchangedKamas[Character.Id]);
                foreach (var item in m_exchangedItems[Character.Id])
                    Character.Inventory.RemoveItem(item.Key, item.Value);
                Character.Inventory.AddKamas(m_reward.RewardedKamas);
                foreach (var item in m_reward.RewardedItems)
                    Character.Inventory.AddItem(item.Template.Create(Character.Id, (int)Character.Type, item.Quantity));
                Character.CachedBuffer = false;
                return true;
            }

            Character.AddMessage(() => Character.AbortAction(GameActionTypeEnum.EXCHANGE));
            return false;
        }

        public override int AddItem(AbstractEntity entity, long guid, int quantity, long price = -1)
        {
            var added = base.AddItem(entity, guid, quantity, price);
            if (added > 0)
            {
                var invItem = Character.Inventory.GetItem(guid);
                if (invItem == null)
                    return 0;
                if (!m_templateQuantity.ContainsKey(invItem.TemplateId))
                    m_templateQuantity.Add(invItem.TemplateId, 0);
                // Acumular la cantidad realmente aportada (base.AddItem la recorta a lo que
                // el jugador posee), nunca la pedida por el cliente: evita duplicar recompensas.
                m_templateQuantity[invItem.TemplateId] += added;

                CheckRewards();

                return added;
            }

            return 0;
        }

        public override int RemoveItem(AbstractEntity entity, long guid, int quantity)
        {
            var removed = base.RemoveItem(entity, guid, quantity);
            if (removed > 0)
            {
                var invItem = Character.Inventory.GetItem(guid);
                if (invItem == null)
                    return 0;
                m_templateQuantity[invItem.TemplateId] -= removed;
                if (m_templateQuantity[invItem.TemplateId] <= 0)
                    m_templateQuantity.Remove(invItem.TemplateId);

                CheckRewards();

                return removed;
            }
            return 0;
        }

        public override long MoveKamas(AbstractEntity entity, long quantity)
        {
            var moved = base.MoveKamas(entity, quantity);

            CheckRewards();

            return moved;
        }

        private void CheckRewards()
        {
            Character.CachedBuffer = true;

            if (m_reward != null)
                foreach (var item in m_reward.RewardedItems)
                    Character.Dispatch(WorldMessage.EXCHANGE_DISTANT_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_REMOVE, item.TemplateId.ToString()));

            m_reward = null;

            Character.Dispatch(WorldMessage.EXCHANGE_DISTANT_MOVEMENT(ExchangeMoveEnum.MOVE_GOLD, OperatorEnum.OPERATOR_ADD, "0"));

            m_reward = (m_templateQuantity.Count > 0 || m_exchangedKamas[Character.Id] > 0) ? Npc.Rewards.Find(entry => entry.Match(m_templateQuantity, m_exchangedKamas[Character.Id])) : null;
            if (m_reward != null)
            {
                foreach (var item in m_reward.RewardedItems)
                    Character.Dispatch(WorldMessage.EXCHANGE_DISTANT_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, item.TemplateId + "|" + item.Quantity + '|' + item.TemplateId + '|' + item.Template.Effects));
                Character.Dispatch(WorldMessage.EXCHANGE_DISTANT_MOVEMENT(ExchangeMoveEnum.MOVE_GOLD, OperatorEnum.OPERATOR_ADD, m_reward.RewardedKamas.ToString()));
            }

            Character.CachedBuffer = false;
        }
    }
}


