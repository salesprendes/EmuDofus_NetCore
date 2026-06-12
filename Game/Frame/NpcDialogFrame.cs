using Protocolo.Framework.Network;
using Game.Action;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Frame
{
    public sealed class NpcDialogFrame : AbstractNetworkFrame<NpcDialogFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'D':
                    switch (message[1])
                    {
                        case 'R':
                            return DialogReply;

                        case 'V':
                            return DialogLeave;

                        default:
                            return null;
                    }

                default:
                    return null;
            }
        }

        public void DialogReply(CharacterEntity character, string message)
        {
            var dialogData = message.AsSpan(2);
            Span<Range> dialogParts = stackalloc Range[3];
            var dialogPartCount = dialogData.Split(dialogParts, '|');
            if (dialogPartCount < 2 || dialogData[dialogParts[1]].IsEmpty)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int questionId = -1;
            int responseId = -1;
            if (!int.TryParse(dialogData[dialogParts[0]], out questionId) || !int.TryParse(dialogData[dialogParts[1]], out responseId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => { ((GameNpcDialogAction)character.CurrentAction).Dialog.ProcessResponse(responseId); });
        }

        public void DialogLeave(CharacterEntity character, string message)
        {
            character.AddMessage(() => { character.StopAction(GameActionTypeEnum.NPC_DIALOG); });
        }
    }
}
