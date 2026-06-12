using Game.Action;
using Game.Entity;
using Game.Entity.Inventory;
using Game.Job;
using Game.Map;
using Game.Network;

namespace Game.Interactive.Type
{
    public sealed class TrashCan : InteractiveObject
    {
        private StorageInventory m_storage;

        public TrashCan(MapInstance map, int cellId)
    : base(map, cellId)
        {
            m_storage = new StorageInventory();
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            switch (skill.Id)
            {
                case SkillIdEnum.SKILL_FOUILLER:
                    StartUse(character);
                    break;
            }
        }

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


