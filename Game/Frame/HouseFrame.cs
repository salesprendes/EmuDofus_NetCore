using Protocolo.Framework.Network;
using Game.Entity;
using Game.Network;
using System;

namespace Game.Frame
{
    public sealed class HouseFrame : AbstractNetworkFrame<HouseFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'h':
                    switch (message[1])
                    {
                        case 'B': return HouseBuy;
                        case 'G': return HouseGuild;
                        case 'Q': return HouseKick;
                        case 'S': return HouseSetPrice;
                        case 'V': return HouseCloseDialog;
                    }
                    break;

                case 'K':
                    switch (message[1])
                    {
                        case 'V': return KeyClose;
                        case 'K':
                            if (message.Length < 3) return null;
                            switch (message[2])
                            {
                                case '0': return KeyEnter;
                                case '1': return KeySet;
                            }
                            break;
                    }
                    break;
            }

            return null;
        }

        // hB — buy the house currently displayed
        private void HouseBuy(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                character.CurrentHouse?.Buy(character);
            });
        }

        // hG[+|-|0|<rights>] — guild sharing
        private void HouseGuild(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                character.CurrentHouse?.SetGuildRights(character, message.Substring(2));
            });
        }

        // hQ<characterId> — kick player from house (currently unsupported client-side for basic houses)
        private void HouseKick(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
            });
        }

        // hS<price> — set sale price (0 = remove from sale)
        private void HouseSetPrice(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!long.TryParse(message.AsSpan(2), out var price))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
                character.CurrentHouse?.SetSalePrice(character, price);
            });
        }

        // hV — close buy dialog
        private void HouseCloseDialog(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                character.CurrentHouse = null;
                character.Dispatch(WorldMessage.HOUSE_CLOSE_BUY_DIALOG());
            });
        }

        // KV — close key dialog
        private void KeyClose(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                character.CurrentHouse = null;
                character.Dispatch(WorldMessage.KEY_CLOSE());
            });
        }

        // KK0|<code> — enter code to access house or chest
        private void KeyEnter(CharacterEntity character, string message)
        {
            // Format: KK0|<code>  (pipe is at index 3)
            var code = message.Length > 4 ? message.Substring(4) : "";
            character.AddMessage(() =>
            {
                character.CurrentHouse?.TryEnter(character, code);
            });
        }

        // KK1|<code> — set new lock code
        private void KeySet(CharacterEntity character, string message)
        {
            var code = message.Length > 4 ? message.Substring(4) : "";
            character.AddMessage(() =>
            {
                character.CurrentHouse?.SetLockCode(character, code);
            });
        }
    }
}
