using Game.Action;
using Game.Entity;
using Game.Fight;
using Game.Network;
using Protocolo.Framework.Network;
using System;

namespace Game.Frame
{
    public sealed class FightPlacementFrame : AbstractNetworkFrame<FightPlacementFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'G':
                    switch (message[1])
                    {
                        case 'R':
                            return FightReady;

                        case 'p':
                            return FightPlacement;

                        case 'Q':
                            return FightQuit;

                        case 'P':
                            if (message.Length < 3)
                                return null;
                            return FightPVPToggle;

                        case 'f':
                            return FightSetFlag;
                    }
                    break;

                case 'f':
                    switch (message[1])
                    {
                        case 'N':
                        case 'S':
                        case 'P':
                        case 'H':
                            return FightOption;
                    }
                    break;
            }

            return null;
        }

        private void FightOption(CharacterEntity character, string message)
        {
            var optionType = (FightOptionTypeEnum)message[1];

            character.AddMessage(() =>
                {
                    if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (!character.IsLeader)
                    {
                        Logger.Debug($"GameFightPlacement::Option un jugador que no es lider ha intentado bloquear opciones: {character.Name}");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.Team.OptionLock(optionType);
                });
        }

        private void FightReady(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.Fight.FighterReady(character);
                });
        }

        private void FightPlacement(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (character.TurnReady)
                    {
                        Logger.Debug($"GameFightPlacement::Placement el jugador ya marco listo y no puede moverse: {character.Name}");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    int cellId = -1;
                    if (!int.TryParse(message.AsSpan(2), out cellId) || cellId < 0)
                    {
                        Logger.Debug($"GameFightPlacement::Placement no se pudo leer la celda indicada: {character.Name}");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.Fight.FighterPlacementChange(character, cellId);
                });
        }

        private void FightQuit(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (message == "GQ")
                {
                    character.Fight.AddMessage(() => character.Fight.FightQuit(character));
                    return;
                }

                if (!character.IsLeader)
                {
                    Logger.Debug($"FightPlacement::Quit un jugador que no es lider ha intentado expulsar a otro: {character.Name}");
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                long fighterId = -1;
                if (!long.TryParse(message.AsSpan(2), out fighterId))
                {
                    Logger.Debug($"FightPlacement::Quit no se pudo leer el id del luchador: {character.Name}");
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var selectedFighter = character.Team.GetFighter(fighterId);
                if (selectedFighter == null || selectedFighter.Type != EntityTypeEnum.TYPE_CHARACTER)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (selectedFighter.IsLeader)
                {
                    Logger.Debug($"FightPlacement::Quit no se puede expulsar al lider: {character.Name}");
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var selectedCharacter = selectedFighter as CharacterEntity;

                character.Fight.AddMessage(() => character.Fight.FightQuit(selectedCharacter, true));
            });
        }

        private void FightPVPToggle(CharacterEntity character, string message)
        {
            switch (message[2])
            {
                case '*':
                    character.SafeDispatch(WorldMessage.ALIGNMENT_DISABLE_COST((character.Honour / 100) * 5));
                    return;

                case '+':
                case '-':
                    character.AddMessage(() =>
                    {
                        if (character.HasGameAction(GameActionTypeEnum.FIGHT) && (character.Fight.State == FightStateEnum.STATE_PLACEMENT || character.Fight.State == FightStateEnum.STATE_FIGHTING))
                        {
                            character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                            return;
                        }

                        if (message[2] == '+')
                            character.EnableAlignment();
                        else
                            character.DisableAlignment();
                    });
                    return;
            }

            character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
        }

        private void FightSetFlag(CharacterEntity character, string message)
        {
            int cellId = -1;
            if (message.Length < 3 || !int.TryParse(message.AsSpan(2), out cellId) || cellId < 0)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                character.Fight.AddMessage(() => character.Fight.Dispatch(WorldMessage.FIGHT_CELL_FLAG(character.Id, cellId)));
            });
        }
    }
}

