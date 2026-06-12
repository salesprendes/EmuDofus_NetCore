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
    public sealed class AddExperienceEffect : AbstractActionEffect<AddExperienceEffect>
    {
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            return Process(character, new Dictionary<string, string>() { { "experience", effect.RandomJet.ToString() } });
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            var experience = long.Parse(parameters["experience"]);

            character.CachedBuffer = true;
            character.AddExperience(experience);
            character.SendAccountStats();
            character.Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_EXPERIENCE_GAINED, experience));
            character.CachedBuffer = false;

            return true;
        }
    }
}


