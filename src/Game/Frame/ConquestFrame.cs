using Protocolo.Framework.Network;
using Game.Action;
using Game.Entity;
using Game.Fight;
using Game.Manager;
using Game.Network;
using System;
using System.Linq;

namespace Game.Frame
{
    public sealed class ConquestFrame : AbstractNetworkFrame<ConquestFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            if (message[0] == 'C')
            {
                switch (message[1])
                {
                    case 'b': return ConquestBalance;
                    case 'B': return ConquestBonus;
                    case 'W': return ConquestWorldInfos;
                    case 'I': return ConquestPrismInfos;
                    case 'F': return ConquestPrismFight;
                }

                return null;
            }

            if (message[0] != 'G' || message[1] != 'c' || message.Length < 3)
                return null;

            switch (message[2])
            {
                case 'P': return ConquestPlace;
                case 'A': return ConquestAttack;
                case 'X': return ConquestDestroy;
            }

            return null;
        }

        private void ConquestBonus(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                var worldBonus = ConquestManager.Instance.GetWorldBalance(character.AlignmentId);
                var rankMultiplicator = ConquestManager.Instance.GetRankMultiplicator(character);
                character.Dispatch(WorldMessage.CONQUEST_BONUS(worldBonus, rankMultiplicator, worldBonus));
            });
        }

        private void ConquestBalance(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                var areaId = character.Map?.SubArea?.Area?.Id ?? 0;
                character.Dispatch(WorldMessage.CONQUEST_BALANCE(
                    ConquestManager.Instance.GetWorldBalance(character.AlignmentId),
                    ConquestManager.Instance.GetAreaBalance(areaId, character.AlignmentId)));
            });
        }

        private void ConquestWorldInfos(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (message.Length < 3 || (message[2] != 'J' && message[2] != 'V'))
                    return;

                character.Dispatch(WorldMessage.CONQUEST_WORLD_DATA(ConquestManager.Instance.SerializeAs_WorldData(character)));
            });
        }

        private void ConquestPrismInfos(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (message.Length < 3)
                    return;

                switch (message[2])
                {
                    case 'J':
                        var territory = ConquestManager.Instance.GetByCharacterMap(character);
                        if (territory == null)
                        {
                            character.Dispatch(WorldMessage.CONQUEST_PRISM_INFOS_ERROR(-3));
                            return;
                        }

                        var fight = territory.CurrentFight;
                        if (fight == null)
                        {
                            character.Dispatch(WorldMessage.CONQUEST_PRISM_INFOS_ERROR(-1));
                            return;
                        }
                        if (fight.State != FightStateEnum.STATE_PLACEMENT)
                        {
                            character.Dispatch(WorldMessage.CONQUEST_PRISM_INFOS_ERROR(-2));
                            return;
                        }

                        var conquestFight = fight as ConquestFight;
                        character.Dispatch(WorldMessage.CONQUEST_PRISM_FIGHT_ATTACKERS(fight, fight.Team0.Fighters.ToArray()));
                        character.Dispatch(WorldMessage.CONQUEST_PRISM_FIGHT_DEFENDERS(fight, (conquestFight != null ? conquestFight.AllDefenders : fight.Team1.Fighters.OfType<CharacterEntity>()).ToArray()));

                        var timer = Math.Max(0, (int)(fight.StartTime - fight.UpdateTime));
                        character.Dispatch(WorldMessage.CONQUEST_PRISM_INFOS_JOINED(timer, WorldConfig.PVP_START_TIMEOUT, 7));
                        break;

                    case 'V':
                        character.Dispatch(WorldMessage.CONQUEST_PRISM_INFOS_CLOSING());
                        break;
                }
            });
        }

        private void ConquestPrismFight(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (message.Length < 3)
                    return;

                if (message[2] == 'V')
                {
                    if (character.HasGameAction(GameActionTypeEnum.PRISM_AGGRESSION))
                        character.StopAction(GameActionTypeEnum.PRISM_AGGRESSION);
                    return;
                }

                if (message[2] != 'J')
                    return;

                var territory = ConquestManager.Instance.GetByCharacterMap(character);
                var conquest = territory?.CurrentFight as ConquestFight;
                if (territory == null || conquest == null || territory.AlignmentId != character.AlignmentId)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (!conquest.CanDefend)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (!character.CanGameAction(GameActionTypeEnum.PRISM_AGGRESSION))
                {
                    character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_YOU_ARE_AWAY));
                    return;
                }

                character.DefendConquest(conquest);

                conquest.AddMessage(() =>
                {
                    if (!conquest.CanDefend)
                    {
                        character.AddMessage(() => character.StopAction(GameActionTypeEnum.PRISM_AGGRESSION));
                        return;
                    }

                    conquest.DefenderJoin(character);
                });
            });
        }

        private void ConquestPlace(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                var subArea = character.Map?.SubArea;
                if (subArea == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (!ConquestManager.Instance.CanPlacePrism(character, subArea.Id))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (!ConquestManager.Instance.PlacePrism(character, subArea.Id))
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
            });
        }

        private void ConquestAttack(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!int.TryParse(message.Substring(3), out int subAreaId))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var territory = ConquestManager.Instance.GetOrCreateNeutralForAttack(subAreaId);
                if (territory == null || !ConquestManager.Instance.CanAttack(territory, character))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var map = character.Map;
                if (map == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                map.AddMessage(() => map.FightManager.StartConquestFight(character, territory));
            });
        }

        private void ConquestDestroy(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!int.TryParse(message.Substring(3), out int subAreaId))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var territory = ConquestManager.Instance.GetBySubArea(subAreaId);
                if (territory == null || character.AlignmentId != territory.AlignmentId)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                ConquestManager.Instance.RemoveTerritory(subAreaId);
            });
        }
    }
}
