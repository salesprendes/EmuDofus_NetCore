using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class RemoveItemEffect : AbstractActionEffect<RemoveItemEffect>
    {
        public override bool ProcessItem(Entity.CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            throw new NotImplementedException();
        }

        public override bool Process(Entity.CharacterEntity character, Dictionary<string, string> parameters)
        {
            var templateId = int.Parse(parameters["templateId"]);

            var item = character.Inventory.Items.Find(entry => entry.TemplateId == templateId);
            if (item == null)
                return false;

            character.Inventory.RemoveItem(item.Id);

            return true;
        }
    }
}


