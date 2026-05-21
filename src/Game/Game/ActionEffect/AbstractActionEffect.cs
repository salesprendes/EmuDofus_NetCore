using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Entity;
using Game.Stats;
using System.Collections.Generic;

namespace Game.ActionEffect
{    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractActionEffect<T> : Singleton<T>, IActionEffect
        where T : AbstractActionEffect<T>, new()
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="item"></param>
        /// <param name="effect"></param>
        /// <param name="targetId"></param>
        /// <param name="targetCell"></param>
        /// <returns></returns>
        public abstract bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="parameters"></param>
        public abstract bool Process(CharacterEntity character, Dictionary<string, string> parameters);
    }
}


