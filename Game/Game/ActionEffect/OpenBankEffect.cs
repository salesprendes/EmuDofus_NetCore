using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.ActionEffect
{
    public sealed class OpenBankEffect : AbstractActionEffect<OpenBankEffect>
    {
        public override bool ProcessItem(CharacterEntity character, Database.Structure.ItemDAO item, Stats.GenericEffect effect, long targetId, int targetCell)
        {
            throw new NotImplementedException();
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            if (!character.CanGameAction(Action.GameActionTypeEnum.EXCHANGE))
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_YOU_ARE_AWAY));
                return false;
            }

            var taxe = character.Bank.Items.GroupBy(item => item.TemplateId).Count();
            if (character.Inventory.Kamas < taxe)
            {
                if (character.Bank.Kamas < taxe)
                {
                    character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_NOT_ENOUGH_KAMAS, taxe));
                    return false;
                }

                character.Bank.SubKamas(taxe);
            }
            else
            {
                character.Inventory.SubKamas(taxe);
            }

            character.CachedBuffer = true;
            character.ExchangeStorage(character.Bank);
            character.CachedBuffer = false;

            return true;
        }
    }
}


