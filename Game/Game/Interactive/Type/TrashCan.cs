using Game.Action;
using Game.Entity;
using Game.Entity.Inventory;
using Game.Job;
using Game.Map;
using Game.Network;

namespace Game.Interactive.Type
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TrashCan : InteractiveObject
    {
        /// <summary>
        /// 
        /// </summary>
        private StorageInventory m_storage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="cellId"></param>
        public TrashCan(MapInstance map, int cellId)
            : base(map, cellId)
        {
            m_storage = new StorageInventory();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="skill"></param>
        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            switch (skill.Id)
            {
                case SkillIdEnum.SKILL_FOUILLER:
                    StartUse(character);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        public void StartUse(CharacterEntity character)
        {
            if (!character.CanGameAction(GameActionTypeEnum.EXCHANGE))
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_YOU_ARE_AWAY));
                return;
            }

            character.ExchangeStorage(m_storage);
        }
    }
}


