using Game.Database.Structure;
using Game.Entity;
using Game.Stats;
using Game.Manager;
using Game.Network;
using System.Collections.Generic;

namespace Game.ActionEffect
{
    public sealed class AddSpellEffect : AbstractActionEffect<AddSpellEffect>
    {
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, new Dictionary<string, string>() { { "spellId", effect.RandomJet.ToString() } });
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            var spellId = int.Parse(parameters["spellId"]);

            if(SpellManager.Instance.GetTemplate(spellId) == null)
                return false;
            
            if (character.SpellBook.HasSpell(spellId))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_UNABLE_LEARN_SPELL, spellId));
                return false;
            }

            character.SpellBook.AddSpell(spellId);
            character.Dispatch(WorldMessage.SPELLS_LIST(character.SpellBook));

            return true;
        }
    }
}


