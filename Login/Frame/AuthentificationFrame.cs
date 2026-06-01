using System;
using Protocolo.Framework.Network;
using Login.Database.Repository;
using Login.Network;

namespace Login.Frames
{
    public sealed class AuthentificationFrame : AbstractNetworkFrame<AuthentificationFrame, AuthClient, string>
    {
        public override Action<AuthClient, string> GetHandler(string message) => message == "Af" ? (Action<AuthClient, string>)HandleQueuePosition : HandleAuthentification;
        private void HandleQueuePosition(AuthClient client, string message) => AuthService.Instance.AddMessage(() => AuthService.Instance.SendQueuePosition(client));


        private void HandleAuthentification(AuthClient client, string message)
        {
            if (client.IsWaitingAuthenticationQueue)
            {
                AuthService.Instance.AddMessage(() => AuthService.Instance.SendQueuePosition(client));
                return;
            }

            var credentials = message.Split('#');
            if (credentials.Length != 2 || credentials[0].Length == 0 || credentials[0].Length > 64 || credentials[1].Length < 2 || credentials[1].Length > 512)
            {
                AuthService.Instance.RegisterFailedAuth(client.Ip);
                client.Send(AuthMessage.AUTH_FAILED_CREDENTIALS());
                return;
            }

            var account = credentials[0];
            var password = credentials[1].Substring(1);

            AuthService.Instance.AddMessage(() => ProcessAuthentification(client, account, password));
        }

        private void ProcessAuthentification(AuthClient client, string accountName, string password)
        {
            var account = AccountRepository.Instance.GetByName(accountName);

            if (account == null || Util.CryptPassword(client.AuthKey, account.Password) != password)
            {
                AuthService.Instance.RegisterFailedAuth(client.Ip);
                client.Send(AuthMessage.AUTH_FAILED_CREDENTIALS());
                return;
            }

            if (account.Banned)
            {
                client.Send(AuthMessage.AUTH_FAILED_BANNED());
                return;
            }

            if (AuthService.Instance.IsConnected(account.Id))
            {
                client.Send(AuthMessage.AUTH_FAILED_ALREADY_CONNECTED());
                return;
            }

            AuthService.Instance.RegisterSuccessfulAuth(client.Ip);

            if (AuthService.Instance.TryQueueAuthentification(client, account))
                return;

            AuthService.Instance.AuthentifyClient(client, account);
        }
    }
}

