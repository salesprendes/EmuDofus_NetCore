using Game.Database.Structure;
using Game.Entity;
using Game.Manager;
using Game.Network;
using Protocolo.Framework.Network;
using System;

namespace Game.Frame
{
    public sealed class InventoryFrame : AbstractNetworkFrame<InventoryFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'O':
                    switch (message[1])
                    {
                        case 'M':
                            return ObjectMove;

                        case 'U':
                            return ObjectUse;

                        case 'D':
                            return ObjectDrop;

                        case 'd':
                            return ObjectDelete;

                        case 'f':
                            return LivingObjectFeed;

                        case 's':
                            return LivingObjectSkin;

                        case 'x':
                            return LivingObjectDissociate;

                        default:
                            break;
                    }
                    break;

                case 'R':
                    switch (message[1])
                    {
                        case 'r':
                            return MountRide;

                        case 'n':
                            return MountRename;

                        case 'x':
                            return MountXpShare;

                        case 'd':
                            return MountData;

                        case 'f':
                            return MountFree;

                        case 'c':
                            return MountCastrate;

                        case 'p':
                            return PaddockInformations;

                        case 'o':
                            return PaddockRemoveObject;

                        case 's':
                            return PaddockSetPrice;

                        case 'b':
                            return PaddockBuy;

                        case 'v':
                            return PaddockLeave;
                    }
                    break;

                default:
                    break;
            }

            return null;
        }

        private void MountRide(CharacterEntity character, string message) => character.AddMessage(character.MountRideUnride);

        // "Rn<nombre>" : renombrar la montura equipada.
        private void MountRename(CharacterEntity character, string message)
        {
            var name = message.Substring(2);
            character.AddMessage(() => character.RenameMount(name));
        }

        // "Rx<porcentaje>" : fijar el % de experiencia compartido con la montura equipada.
        private void MountXpShare(CharacterEntity character, string message)
        {
            if (!int.TryParse(message.AsSpan(2), out var percent))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.SetMountXpShare(percent));
        }

        // "Rd<idMontura>|<tiempo>" : pedir la ficha detallada de una montura para el visor.
        private void MountData(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            var separator = data.IndexOf('|');
            var idSpan = separator >= 0 ? data.Slice(0, separator) : data;
            if (!long.TryParse(idSpan, out var mountId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.SendMountData(mountId));
        }

        // "Rf" : liberar la montura equipada.
        private void MountFree(CharacterEntity character, string message) => character.AddMessage(character.FreeMount);

        // "Rc" : castrar la montura equipada.
        private void MountCastrate(CharacterEntity character, string message) => character.AddMessage(character.CastrateMount);

        // "Rp<idSprite>" : pedir la informacion del enclos del mapa actual.
        private void PaddockInformations(CharacterEntity character, string message) => character.AddMessage(character.SendCurrentPaddockInformations);

        // "Rv" : cerrar el dialogo de compra/venta del enclos.
        private void PaddockLeave(CharacterEntity character, string message) => character.AddMessage(character.PaddockLeave);

        // "Ro<celda>" : retirar la montura colocada en una celda del enclos.
        private void PaddockRemoveObject(CharacterEntity character, string message)
        {
            if (!int.TryParse(message.AsSpan(2), out var cellId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.PaddockRemoveObject(cellId));
        }

        // "Rs<precio>" : fijar el precio de venta del enclos.
        private void PaddockSetPrice(CharacterEntity character, string message)
        {
            if (!long.TryParse(message.AsSpan(2), out var price))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.PaddockSetPrice(price));
        }

        // "Rb<precio>" : comprar el enclos.
        private void PaddockBuy(CharacterEntity character, string message)
        {
            if (!long.TryParse(message.AsSpan(2), out var price))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.PaddockBuy(price));
        }


        private void ObjectMove(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[4];
            var partCount = data.Split(parts, '|');
            if (partCount < 2)
            {
                character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                return;
            }

            long itemId = -1;
            if (!long.TryParse(data[parts[0]], out itemId))
            {
                character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                return;
            }

            int slotId = -1;
            if (!int.TryParse(data[parts[1]], out slotId))
            {
                character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                return;
            }

            int quantity = 1;
            if (partCount > 2)
            {
                if (!int.TryParse(data[parts[2]], out quantity))
                {
                    character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                    return;
                }
            }

            if (quantity <= 0)
            {
                character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                return;
            }

            if (!Enum.IsDefined(typeof(ItemSlotEnum), slotId))
            {
                character.SafeDispatch(WorldMessage.OBJECT_MOVE_ERROR());
                return;
            }

            character.AddMessage(() =>
                {
                    var item = character.Inventory.Items.Find(x => x.Id == itemId);
                    if (item == null)
                    {
                        character.Dispatch(WorldMessage.OBJECT_MOVE_ERROR());
                        return;
                    }

                    character.Inventory.MoveItem(item, (ItemSlotEnum)slotId, quantity);
                });
        }

        private void ObjectUse(CharacterEntity character, string message)
        {
            var useData = message.AsSpan(2);
            Span<Range> useParts = stackalloc Range[4];
            var usePartCount = useData.Split(useParts, '|', StringSplitOptions.RemoveEmptyEntries);

            long itemId = -1;
            if (usePartCount < 1 || !long.TryParse(useData[useParts[0]], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long targetId = -1;
            if (usePartCount > 1)
            {
                if (!long.TryParse(useData[useParts[1]], out targetId))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }

            int targetCell = -1;
            if (usePartCount > 2)
            {
                if (!int.TryParse(useData[useParts[2]], out targetCell))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }

            character.AddMessage(() => { ActionEffectManager.Instance.ApplyEffects(character, itemId, targetId, targetCell); });
        }

        private void LivingObjectFeed(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[4];
            var partCount = data.Split(parts, '|');
            if (partCount < 3)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long itemId = -1;
            if (!long.TryParse(data[parts[0]], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long foodItemId = -1;
            if (!long.TryParse(data[parts[2]], out foodItemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.Inventory.FeedLivingItem(itemId, foodItemId));
        }

        private void LivingObjectSkin(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[4];
            int partCount = data.Split(parts, '|');

            long itemId = -1;
            if (partCount < 1 || !long.TryParse(data[parts[0]], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int skinId = 0;
            if (partCount >= 3)
                int.TryParse(data[parts[2]], out skinId);

            character.AddMessage(() => character.Inventory.SetLivingItemSkin(itemId, skinId));
        }

        private void LivingObjectDissociate(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[2];
            var partCount = data.Split(parts, '|');

            long itemId = -1;
            if (partCount < 1 || !long.TryParse(data[parts[0]], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.Inventory.DissociateLivingItem(itemId));
        }

        private void ObjectDrop(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[2];
            var partCount = data.Split(parts, '|');

            long itemId = -1;
            if (partCount < 1 || !long.TryParse(data[parts[0]], out itemId))
            {
                character.SafeDispatch(WorldMessage.OBJECT_DROP_ERROR_CANT_DROP());
                return;
            }

            int quantity = 1;
            if (partCount > 1)
                int.TryParse(data[parts[1]], out quantity);

            if (quantity <= 0)
                quantity = 1;

            character.AddMessage(() =>
            {
                var item = character.Inventory.Items.Find(x => x.Id == itemId);
                if (item == null)
                {
                    character.Dispatch(WorldMessage.OBJECT_DROP_ERROR_CANT_DROP());
                    return;
                }
                character.Inventory.RemoveItem(itemId, quantity);
                character.Dispatch(WorldMessage.OBJECT_DROP_SUCCESS());
            });
        }

        private void ObjectDelete(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[3];
            var partCount = data.Split(parts, '|');

            long itemId = -1;
            if (partCount < 1 || !long.TryParse(data[parts[0]], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int quantity = 1;
            if (partCount > 1)
            {
                if (!int.TryParse(data[parts[1]], out quantity))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }

            if (quantity <= 0)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => { character.Inventory.RemoveItem(itemId, quantity); });
        }
    }
}
