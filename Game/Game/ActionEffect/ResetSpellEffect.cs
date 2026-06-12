using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class ResetSpellEffect : AbstractActionEffect<ResetSpellEffect>
    {
        public override bool ProcessItem(Entity.CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, null);
        }

        public override bool Process(Entity.CharacterEntity character, Dictionary<string, string> parameters)
        {
            character.SpellBook.Spells.ForEach(spell => spell.Level = 1);
            character.SpellPoint = character.Level - 1;

            character.CachedBuffer = true;
            character.SendAccountStats();
            character.Dispatch(WorldMessage.SPELLS_LIST(character.SpellBook));
            character.CachedBuffer = false;

            return true;
        }
    }
}


