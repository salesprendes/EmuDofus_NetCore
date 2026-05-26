using System;
using Protocolo.Framework.Network;
using Game;
using Game.Entity;
using Game.Fight;
using Game.Action;
using Game.Network;

namespace Game.Frame
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class FightFrame : AbstractNetworkFrame<FightFrame, CharacterEntity, string>
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

            switch (message[0])
            {
                case 'G':
                    switch (message[1])
                    {
                        case 't':
                            return FightTurnPass;

                        case 'T':
                            return FightTurnReady;

                        case 'Q':
                            return FightQuit;

                        case 'F':
                            return FightFreeMySoul;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="message"></param>
        private void FightOption(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    if (!character.IsLeader)
                    {
                        Logger.Debug("GameFight::Option non leader player wants to lock : " + character.Name);
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.Team.OptionLock((FightOptionTypeEnum)message[1]);
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="message"></param>
        private void FightTurnReady(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (character.IsSpectating)
                {
                    Logger.Debug("GameFight::TurnReady spectator player cant be ready : " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                character.TurnReady = true;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="message"></param>
        private void FightTurnPass(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (character.IsSpectating)
                {
                    Logger.Debug("GameFight::TurnPass spectator player cant pass turn : " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (character.Fight.CurrentFighter != character)
                {
                    Logger.Debug("GameFight::TurnPass not the turn of this player : " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                character.TurnPass = true;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="message"></param>
        private void FightQuit(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (!character.Fight.CancelButton && character.Fight.State != FightStateEnum.STATE_FIGHTING)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                character.Fight.AddMessage(() => character.Fight.FightQuit(character));
            });
        }

        private void FightFreeMySoul(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (character.Fight.State != FightStateEnum.STATE_FIGHTING)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (!character.IsFighterDead)
                {
                    Logger.Debug("FightFrame::FreeMySoul character is not dead : " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                character.Fight.AddMessage(() => character.Fight.FightQuit(character));
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
                    character.AddMessage(() => character.EnableAlignment());
                    return;
                case '-':
                    character.AddMessage(() => character.DisableAlignment());
                    return;
            }

            character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
        }

        private void FightSetFlag(CharacterEntity character, string message)
        {
            int cellId = -1;
            if (message.Length < 3 || !int.TryParse(message.Substring(2), out cellId) || cellId < 0)
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

                character.Fight.AddMessage(() =>
                    character.Fight.Dispatch(WorldMessage.FIGHT_CELL_FLAG(cellId, character.Id)));
            });
        }
    }
}



