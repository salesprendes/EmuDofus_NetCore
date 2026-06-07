using Game.Action;
using Game.Entity;
using Game.Network;
using Protocolo.Framework.Network;
using System;

namespace Game.Frame
{
    public sealed class GameCreationFrame : AbstractNetworkFrame<GameCreationFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.StartsWith("GC"))
                return GameCreation;
            return null;
        }

        private void GameCreation(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                character.FrameManager.RemoveFrame(Instance);
                character.FrameManager.AddFrame(GameInformationFrame.Instance);

                var map = character.Map;
                if (map == null)
                {
                    character.MapId = WorldConfig.GetStartMap(character.Breed);
                    character.CellId = WorldConfig.GetStartCell(character.Breed);
                    map = character.Map;
                }

                character.CachedBuffer = true;
                character.Dispatch(WorldMessage.GAME_CREATION_SUCCESS());

                if (character.HasGameAction(GameActionTypeEnum.FIGHT))
                    character.Dispatch(WorldMessage.GAME_DATA_MAP(character.Fight.Map.Id, character.Fight.Map.CreateTime, character.Fight.Map.DataKey));
                else
                    character.Dispatch(WorldMessage.GAME_DATA_MAP(map.Id, map.CreateTime, map.DataKey));

                character.Dispatch(WorldMessage.ACCOUNT_STATS(character));
                character.CachedBuffer = false;
            });
        }
    }
}



