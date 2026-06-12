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
    public sealed class AddSpellpointEffect : AbstractActionEffect<AddSpellpointEffect>
    {
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, new Dictionary<string, string>() { { "spellpoint", effect.RandomJet.ToString() } });
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            var value = int.Parse(parameters["spellpoint"]);

            character.CachedBuffer = true;
            character.SpellPoint += value;
            character.SendAccountStats();
            character.CachedBuffer = false;

            return true;
        }
    }
}


