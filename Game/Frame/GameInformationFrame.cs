using System;
using Protocolo.Framework.Network;
using Game.Action;
using Game.Entity;
using Game.Fight;

namespace Game.Frame
{
    public sealed class GameInformationFrame : AbstractNetworkFrame<GameInformationFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message == "GI")
                return GameInformation;
            return null;
        }

        private void GameInformation(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    character.FrameManager.RemoveFrame(GameInformationFrame.Instance);

                    if (character.HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        character.Fight.SendFightJoinInfos(character);
                        return;
                    }

                    WorldService.Instance.RemoveUpdatable(character);

                    character.Map.AddMessage(() => character.StartAction(GameActionTypeEnum.MAP));
                });
        }
    }
}


