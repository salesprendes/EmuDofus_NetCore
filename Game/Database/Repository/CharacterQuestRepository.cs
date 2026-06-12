using Game.Database.Structure;
using Protocolo.Framework.Database;

namespace Game.Database.Repository
{
    public sealed class CharacterQuestRepository : Repository<CharacterQuestRepository, CharacterQuestDAO>
    {
        public override void OnObjectAdded(CharacterQuestDAO obj)
        {
            CharacterRepository.Instance.GetById(obj.Id).AddQuest(obj);
        }
    }
}

