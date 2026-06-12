using Protocolo.Framework.Network;
using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Frame
{
    public sealed class GameTombestoneFrame : AbstractNetworkFrame<GameTombestoneFrame, CharacterEntity, string>
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
                        case 'F':
                            return FreeSoul;
                    }
                    break;
            }

            return null;
        }

        public void FreeSoul(CharacterEntity character, string message)
        {
            character.FrameManager.RemoveFrame(GameTombestoneFrame.Instance);

            character.AddMessage(character.FreeSoul);
        }
    }
}


