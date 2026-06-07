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
                        Logger.Debug("GameFight::Option un jugador que no es lider ha intentado bloquear opciones: " + character.Name);
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

                // GT is the in-fight turn-transition ACK. Placement readiness goes through
                // GR (FighterReady), so reject it outside the fighting phase to avoid
                // silently flipping TurnReady during placement or end states.
                if (character.Fight.State != FightStateEnum.STATE_FIGHTING)
                {
                    Logger.Debug("GameFight::TurnReady el combate no esta en fase de lucha: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (character.IsSpectating)
                {
                    Logger.Debug("GameFight::TurnReady un espectador no puede marcarse listo: " + character.Name);
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
                    Logger.Debug("GameFight::TurnPass un espectador no puede pasar turno: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (character.Fight.CurrentFighter != character)
                {
                    Logger.Debug("GameFight::TurnPass no es el turno de este jugador: " + character.Name);
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
                    Logger.Debug("FightFrame::FreeMySoul el personaje no esta muerto: " + character.Name);
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
                case '-':
                    character.AddMessage(() =>
                    {
                        if (character.HasGameAction(GameActionTypeEnum.FIGHT) && character.Fight.State == FightStateEnum.STATE_FIGHTING)
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

                character.Fight.AddMessage(() => character.Fight.Dispatch(WorldMessage.FIGHT_CELL_FLAG(cellId, character.Id)));
            });
        }
    }
}


