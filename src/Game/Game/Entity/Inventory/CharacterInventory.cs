using Game.Database.Structure;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entity.Inventory
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class CharacterInventory : EntityInventory
    {
        /// <summary>
        /// 
        /// </summary>
        public CharacterEntity Character
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        public CharacterInventory(CharacterEntity character)
            : base(character, (int)EntityTypeEnum.TYPE_CHARACTER, character.Id)
        {
            Character = character;
            Initialize();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        public override void OnItemAdded(ItemDAO item)
        {
            item.RefreshTemporaryExchangeLock();
            Dispatch(WorldMessage.OBJECT_ADD_SUCCESS(item));
            if ((ItemTypeEnum)item.Template.Type == ItemTypeEnum.TYPE_OBJET_VIVANT)
                Dispatch(WorldMessage.LIVING_ITEM_UPDATE(item));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        public override void OnItemQuantity(long itemId, int quantity)
        {
            Dispatch(WorldMessage.OBJECT_QUANTITY_UPDATE(itemId, quantity));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        public override void OnItemRemoved(long itemId)
        {
            Dispatch(WorldMessage.OBJECT_REMOVE_SUCCESS(itemId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void OnKamasAdded(long value)
        {
            Dispatch(WorldMessage.ACCOUNT_STATS(Character));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void OnKamasSubstracted(long value)
        {
            Dispatch(WorldMessage.ACCOUNT_STATS(Character));
        }
    }
}
