using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class ChangeAlignmentEffect : AbstractActionEffect<ChangeAlignmentEffect>
    {
        public override bool ProcessItem(Entity.CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, new Dictionary<string, string>() { { "alignmentId", effect.Value1.ToString() } });
        }

        public override bool Process(Entity.CharacterEntity character, Dictionary<string, string> parameters)
        {
            var alignmentId = int.Parse(parameters["alignmentId"]);

            character.SetAlignment(alignmentId);

            return true;
        }
    }
}


