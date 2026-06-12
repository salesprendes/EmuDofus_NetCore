using Game.Manager;
using Game.Network;
using Protocolo.Framework.Network;
using System;

namespace Game.Frame
{
    public sealed class AuthentificationFrame : AbstractNetworkFrame<AuthentificationFrame, WorldClient, string>
    {
        public override Action<WorldClient, string> GetHandler(string message)
        {
            return HandleTicket;
        }


        private void HandleTicket(WorldClient client, string message)
        {
            var ticket = message.AsSpan(2).ToString();

            client.FrameManager.RemoveFrame(AuthentificationFrame.Instance);

            WorldService.Instance.AddMessage(() =>
                {
                    var account = ClientManager.Instance.GetAccountTicket(ticket);
                    if (account == null)
                    {
                        client.Send(WorldMessage.ACCOUNT_TICKET_ERROR());
                        return;
                    }

                    WorldService.Instance.AddMessage(() =>
                        {
                            client.FrameManager.AddFrame(CharacterSelectionFrame.Instance);
                            client.Account = account;
                            ClientManager.Instance.ClientAuthentified(client);
                            client.Send(WorldMessage.ACCOUNT_TICKET_SUCCESS());
                        });
                });
        }
    }
}


