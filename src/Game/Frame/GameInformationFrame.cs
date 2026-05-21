using System;
using Protocolo.Framework.Network;
using Game.Action;
using Game.Entity;
using Game.Fight;

namespace Game.Frame
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class GameInformationFrame : AbstractNetworkFrame<GameInformationFrame, CharacterEntity, string>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message == "GI")
                return GameInformation;
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
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

                    character.StartAction(GameActionTypeEnum.MAP);
                });
        }
    }
}


