using System;
using Protocolo.Framework.Network;
using Game.Database.Structure;
using Game;
using Game.Entity;
using Game.Network;
using Game.Manager;

namespace Game.Frame
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class InventoryFrame : AbstractNetworkFrame<InventoryFrame, CharacterEntity, string>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch(message[0])
            {
                case 'O':
                    switch (message[1])
                    {
                        case 'M':
                            return ObjectMove;

                        case 'U':
                            return ObjectUse;

                        case 'd':
                            return ObjectDelete;

                        case 'f':
                            return LivingObjectFeed;

                        case 's':
                            return LivingObjectSkin;

                        case 'x':
                            return LivingObjectDissociate;
                    }
                    break;

                case 'R':
                    switch (message[1])
                    {
                        case 'r':
                            return MountRide;     
                    }
                    break;
            }

            return null;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void MountRide(CharacterEntity character, string message)
        {
            character.AddMessage(character.MountRideUnride);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void ObjectMove(CharacterEntity character, string message)
        {            
            var data = message.Substring(2).Split('|');

            long itemId = -1;
            if(!long.TryParse(data[0], out itemId))
            {
                character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                return;
            }

            int slotId = -1;
            if(!int.TryParse(data[1], out slotId))
            {
                character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                return;
            }

            int quantity = 1;
            if(data.Length > 2)
            {
                if (!int.TryParse(data[2], out quantity))
                {
                    character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                    return;
                }
            }

            if(!Enum.IsDefined(typeof(ItemSlotEnum), slotId))
            {
                character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                return;
            }

            character.AddMessage(() =>
                {
                    var item = character.Inventory.Items.Find(x => x.Id == itemId);
                    if(item == null)
                    {
                        character.Dispatch(WorldMessage.OBJECT_MOVE_ERROR());
                        return;
                    }

                    character.Inventory.MoveItem(item, (ItemSlotEnum)slotId, quantity);
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void ObjectUse(CharacterEntity character, string message)
        {
            var data = message.Substring(2);
            var useData = data.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            long itemId = -1;
            if (!long.TryParse(useData[0], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long targetId = -1;
            if (useData.Length > 1)
            {
                if (!long.TryParse(useData[1], out targetId))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }

            int targetCell = -1;
            if (useData.Length > 2)
            {
                if (!int.TryParse(useData[2], out targetCell))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }

            character.AddMessage(() => 
                {
                    ActionEffectManager.Instance.ApplyEffects(character, itemId, targetId, targetCell);
                });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void LivingObjectFeed(CharacterEntity character, string message)
        {
            var data = message.Substring(2).Split('|');

            long itemId = -1;
            if (!long.TryParse(data[0], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long foodItemId = -1;
            if (data.Length < 3 || !long.TryParse(data[2], out foodItemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.Inventory.FeedLivingItem(itemId, foodItemId));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void LivingObjectSkin(CharacterEntity character, string message)
        {
            var data = message.Substring(2).Split('|');

            long itemId = -1;
            if (!long.TryParse(data[0], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int skinId = 0;
            if (data.Length >= 3)
                int.TryParse(data[2], out skinId);

            character.AddMessage(() => character.Inventory.SetLivingItemSkin(itemId, skinId));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void LivingObjectDissociate(CharacterEntity character, string message)
        {
            var data = message.Substring(2).Split('|');

            long itemId = -1;
            if (!long.TryParse(data[0], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.Inventory.DissociateLivingItem(itemId));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void ObjectDelete(CharacterEntity character, string message)
        {
            var data = message.Substring(2).Split('|');

            long itemId = -1;
            if (!long.TryParse(data[0], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            
            int quantity = 1;
            if (data.Length > 1)
            {
                if (!int.TryParse(data[1], out quantity))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }

            character.AddMessage(() =>
                {
                    character.Inventory.RemoveItem(itemId, quantity);
                });
        }
    }
}


