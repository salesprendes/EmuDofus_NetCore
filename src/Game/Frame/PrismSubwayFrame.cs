using Protocolo.Framework.Network;
using Game.Action;
using Game.Conquest;
using Game.Entity;
using Game.Manager;
using Game.Network;
using System;
using System.Linq;

namespace Game.Frame
{
    public sealed class PrismSubwayFrame : AbstractNetworkFrame<PrismSubwayFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            switch (message[1])
            {
                case 'p': return PrismUse;
                case 'w': return PrismLeave;
            }
            return null;
        }

        private void PrismLeave(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                character.StopAction(GameActionTypeEnum.PRISM_USE);
            });
        }

        private void PrismUse(CharacterEntity character, string message)
        {
            int destMapId;
            if (!int.TryParse(message.Substring(2), out destMapId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                var prismAction = character.CurrentAction as GamePrismSubwayAction;
                if (prismAction == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (destMapId == character.MapId)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                // Solo prismas SubArea del mismo alineamiento son destinos validos
                var destTerritory = ConquestManager.Instance.Territories.FirstOrDefault(t =>
                    t.PrismType == ConquestPrismType.SubArea
                    && t.AlignmentId == character.AlignmentId
                    && t.PrismMapId == destMapId
                    && t.PrismMapId > 0);

                if (destTerritory == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var destMap = MapManager.Instance.GetById(destMapId);
                if (destMap == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                character.StopAction(GameActionTypeEnum.PRISM_USE);

                var nearestCell = destMap.GetNearestCell(destTerritory.PrismCellId);
                if (nearestCell == -1)
                    nearestCell = destTerritory.PrismCellId;

                character.Teleport(destMap.Id, nearestCell);
            });
        }
    }
}
