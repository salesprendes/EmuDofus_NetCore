using Game.Database.Structure;
using Game.Entity;
using Game.Network;
using Game.Stats;
using System.Collections.Generic;

namespace Game.ActionEffect
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AddLifeEffect : AbstractActionEffect<AddLifeEffect>
    {
        const int EMOTE_EAT_REST = 17;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="item"></param>
        /// <param name="effect"></param>
        /// <param name="targetId"></param>
        /// <param name="targetCell"></param>
        /// <returns></returns>
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            if (targetId != -1)
            {
                var entity = character.Map.GetEntity(targetId);
                character = entity as CharacterEntity;
                if (character == null)
                {
                    return false;
                }
            }

            switch ((ItemTypeEnum)item.Template.Type)
            {
                case ItemTypeEnum.TYPE_PAIN:
                    character.EmoteUse(EMOTE_EAT_REST);
                    break;
            }

            return Process(character, new Dictionary<string, string> { { "life", effect.RandomJet.ToString() } });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            if (character.Life == character.MaxLife)
            {
                return false;
            }

            var heal = int.Parse(parameters["life"]);
            if (character.Life + heal > character.MaxLife)
            {
                heal = character.MaxLife - character.Life;
            }

            character.CachedBuffer = true;
            character.Life += heal;
            character.SendAccountStats();
            character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_LIFE_RECOVERED, heal));
            character.CachedBuffer = false;

            return true;
        }
    }
}


