using Game.Entity;
using Game.Network;
using Protocolo.Framework.Network;
using System;

namespace Game.Frame
{
    public sealed class MapFrame : AbstractNetworkFrame<MapFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
            {
                return null;
            }

            switch (message[0])
            {
                case 'e':
                    switch (message[1])
                    {
                        case 'D': // onDirection
                            return EmoteDirection;

                        case 'U': // onUse
                            return EmoteUse;
                    }
                    break;

                case 'f':
                    switch (message[1])
                    {

                        case 'L':
                            return FightList;

                        case 'D':
                            return FightDetails;
                    }
                    break;

                case 'G':
                    switch (message[1])
                    {
                        case 'P': // alignment
                            if (message.Length < 3)
                            {
                                return null;
                            }

                            switch (message[2])
                            {
                                case '+':
                                    return AlignmentEnable;

                                case '-':
                                    return AlignmentDisable;

                                case '*':
                                    return AlignmentDisableCost;
                            }
                            break;
                    }
                    break;
            }

            return null;
        }

        private void EmoteDirection(CharacterEntity character, string message)
        {
            int direction = -1;
            if (!int.TryParse(message.Substring(2), out direction))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
                {
                    character.ChangeDirection(direction);
                });
        }

        private void EmoteUse(CharacterEntity character, string message)
        {
            int emoteId = -1;
            if (!int.TryParse(message.Substring(2), out emoteId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
                {
                    character.EmoteUse(emoteId);
                });
        }

        private void AlignmentDisableCost(CharacterEntity character, string message)
        {
            character.SafeDispatch(WorldMessage.ALIGNMENT_DISABLE_COST((character.Honour / 100) * 5));
        }

        private void AlignmentDisable(CharacterEntity character, string message)
        {
            character.AddMessage(() => character.DisableAlignment());
        }

        private void AlignmentEnable(CharacterEntity character, string message)
        {
            character.AddMessage(() => character.EnableAlignment());
        }

        private void FightList(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (character.Map.FightManager.FightCount > 0)
                {
                    character.Dispatch(WorldMessage.FIGHT_LIST(character.Map.FightManager.Fights));
                }
            });
        }

        private void FightDetails(CharacterEntity character, string message)
        {
            if (message.Length < 3)
            {
                return;
            }

            int fightId = -1;
            if (!int.TryParse(message.Substring(2), out fightId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                var fight = character.Map.FightManager.GetFight(fightId);

                if (fight == null)
                {
                    return;
                }

                character.Dispatch(WorldMessage.FIGHT_DETAILS(fight));
            });
        }
    }
}



