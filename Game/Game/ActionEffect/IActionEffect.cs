using Game.Database.Structure;
using Game.Entity;
using Game.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public interface IActionEffect
    {
        bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell);

        bool Process(CharacterEntity character, Dictionary<string, string> parameters);
    }
}


