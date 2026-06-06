using System;

namespace Protocolo.RPC.Service
{
    internal static class RpcFraming
    {
        internal static void ValidateMessageLength(int length, int maxLength)
        {
            if (length <= 0 || length > maxLength)
                throw new InvalidOperationException("La longitud del mensaje RPC esta fuera de rango: " + length);
        }
    }
}
