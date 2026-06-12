using Game.Database.Structure;
using Game.Entity;
using Game.Stats;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class AddEnergyEffect : AbstractActionEffect<AddEnergyEffect>
    {
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, new Dictionary<string, string>() { { "energy", effect.RandomJet.ToString() } });
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            var energy = int.Parse(parameters["energy"]);

            character.CachedBuffer = true;
            character.Energy += energy;
            character.SendAccountStats();
            character.CachedBuffer = false;

            return true;
        }
    }
}


