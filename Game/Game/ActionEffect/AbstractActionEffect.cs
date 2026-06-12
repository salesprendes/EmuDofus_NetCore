using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Entity;
using Game.Stats;
using System.Collections.Generic;

namespace Game.ActionEffect
{
    public abstract class AbstractActionEffect<T> : Singleton<T>, IActionEffect where T : AbstractActionEffect<T>, new()
    {
        public abstract bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell);

        public abstract bool Process(CharacterEntity character, Dictionary<string, string> parameters);
    }
}


