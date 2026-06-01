using System;
using Protocolo.Framework.Network;
using Protocolo.RPC.Protocol;
using Login.Network;

namespace Login.Frames
{
    public sealed class WorldSelectionFrame : AbstractNetworkFrame<WorldSelectionFrame, AuthClient, string>
    {
        public override Action<AuthClient, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'A':
                    if (message[1] == 'x')
                        return WorldCharacterList;
                    else if (message[1] == 'X')
                        return WorldSelection;
                    break;
            }

            return null;
        }

        private void WorldCharacterList(AuthClient client, string message)
        {
            AuthService.Instance.SendWorldCharacterList(client);
        }

        private void WorldSelection(AuthClient client, string message)
        {
            if (message.Length <= 2 || !int.TryParse(message.Substring(2), out var worldId))
            {
                client.Send(AuthMessage.WORLD_SELECTION_FAILED());
                return;
            }

            AuthService.Instance.AddMessage(() =>
            {
                var worldServer = AuthService.Instance.GetGameServerById(worldId);
                var worldConnection = AuthService.Instance.GetWorldConnectionById(worldId);
                
                if (worldServer == null || worldServer.State != (int)GameStateEnum.ONLINE || worldConnection == null)
                {
                    client.Send(AuthMessage.WORLD_SELECTION_FAILED());
                    return;
                }
                
                client.FrameManager.RemoveFrame(WorldSelectionFrame.Instance);
                var ticket = Util.GenerateString(10);
                
                client.Ticket = ticket;
                worldConnection.Send(new GameTicketMessage(client.Account.Id, client.Account.Name, client.Account.Pseudo, client.Account.Power, client.Account.RemainingSubscription.ToBinary(), client.Account.LastConnectionDate.ToBinary(), client.Account.LastConnectionIP, ticket));
                worldConnection.Players.Add(client.Account.Id);
                
                client.Account.LastConnectionDate = DateTime.Now;
                client.Account.LastConnectionIP = client.Ip;
                
                client.Send(AuthMessage.WORLD_SELECTION_SUCCESS(worldServer.Ip, worldServer.Port, client.Ticket));
            });
        }
    }
}
