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

            // El cliente envía "login\n#1<pwdCifrado>"; el framing descarta el '\n', así que
            // aquí llega "login#1<pwdCifrado>". El cifrado solo usa el alfabeto HASH (sin '#'),
            // por lo que el último "#1" es siempre el prefijo del password, incluso si el
            // nombre de cuenta contiene '#'.
            var separator = message.LastIndexOf("#1", StringComparison.Ordinal);
            if (separator < 1)
            {
                AuthService.Instance.RegisterFailedAuth(client.Ip);
                client.Send(AuthMessage.AUTH_FAILED_CREDENTIALS());
                return;
            }

            var account = message.Substring(0, separator);
            var password = message.Substring(separator + 2);

            if (account.Length > 64 || password.Length == 0 || password.Length > 512)
            {
                AuthService.Instance.RegisterFailedAuth(client.Ip);
                client.Send(AuthMessage.AUTH_FAILED_CREDENTIALS());
                return;
            }

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

