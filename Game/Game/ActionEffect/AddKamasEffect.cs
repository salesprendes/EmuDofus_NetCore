using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class AddKamasEffect : AbstractActionEffect<AddKamasEffect>
    {
        public override bool ProcessItem(Entity.CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, new Dictionary<string, string> { { "kamas", effect.RandomJet.ToString() } });
        }

        public override bool Process(Entity.CharacterEntity character, Dictionary<string, string> parameters)
        {
            var kamas = long.Parse(parameters["kamas"]);

            character.CachedBuffer = true;
            character.Inventory.AddKamas(kamas);
            character.CachedBuffer = false;
            return true;
        }
    }
}


