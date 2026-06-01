using Game.Database.Repository;
using Game.Network;
using System;
using System.Collections.Generic;

namespace Game.ActionEffect
{
    public sealed class AddItemEffect : AbstractActionEffect<AddItemEffect>
    {
        /// <param name="character"></param>
        /// <param name="item"></param>
        /// <param name="effect"></param>
        /// <param name="targetId"></param>
        /// <param name="targetCell"></param>
        /// <returns></returns>
        public override bool ProcessItem(Entity.CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            throw new NotImplementedException();
        }

        /// <param name="character"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override bool Process(Entity.CharacterEntity character, Dictionary<string, string> parameters)
        {
            var itemId = int.Parse(parameters["itemId"]);
            var template = ItemTemplateRepository.Instance.GetById(itemId);
            if (template == null)
                return false;

            character.CachedBuffer = true;
            character.Inventory.AddItem(template.Create(character.Id, (int)character.Type));
            character.Dispatch(WorldMessage.SERVER_INFO_MESSAGE("Item " + template.Name + " added in your inventory."));
            character.CachedBuffer = false;

            return true;
        }
    }
}


