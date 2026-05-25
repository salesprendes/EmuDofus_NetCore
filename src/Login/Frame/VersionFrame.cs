using System;
using Protocolo.Framework.Configuration;
using Protocolo.Framework.Network;
using Login.Network;

namespace Login.Frames
{
    public sealed class VersionFrame : AbstractNetworkFrame<VersionFrame, AuthClient, string>
    {
        [Configurable("ClientVersion")]
        public static string ClientVersion = "1.29.1";

        public override Action<AuthClient, string> GetHandler(string message)
        {
            return HandleVersion;
        }

        private void HandleVersion(AuthClient client, string message)
        {
            client.FrameManager.RemoveFrame(VersionFrame.Instance);

            if (message != ClientVersion)
            {
                client.Send(AuthMessage.PROTOCOL_REQUIRED());
                return;
            }

            client.FrameManager.AddFrame(AuthentificationFrame.Instance);
        }
    }
}

