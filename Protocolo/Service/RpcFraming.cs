using System;
using Protocolo.Framework.IO;

namespace Protocolo.RPC.Service
{
    internal static class RpcFraming
    {
        internal static void ValidateMessageLength(int length, int maxLength)
        {
            if (length <= 0 || length > maxLength)
                throw new InvalidOperationException($"La longitud del mensaje RPC esta fuera de rango: {length}");
        }

        internal static bool TryReadMessage(
            BinaryQueue data,
            RpcMessageBuilder builder,
            int maxLength,
            ref int messageLength,
            ref int messageId,
            out AbstractRcpMessage message)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            message = null;

            if (messageLength == -1)
            {
                if (data.Count < sizeof(int))
                    return false;

                messageLength = data.ReadInt();
                ValidateMessageLength(messageLength, maxLength);
            }

            if (messageId == -1)
            {
                if (data.Count < sizeof(int))
                    return false;

                messageId = data.ReadInt();
            }

            if (data.Count < messageLength)
                return false;

            message = builder.BuildMessage(messageId, data, messageLength);
            messageId = -1;
            messageLength = -1;
            return true;
        }
    }
}
