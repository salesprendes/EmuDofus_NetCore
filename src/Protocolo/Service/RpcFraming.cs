using System;

namespace Protocolo.RPC.Service
{
    internal static class RpcFraming
    {
        internal static void ValidateMessageLength(int length, int maxLength)
        {
            if (length <= 0 || length > maxLength)
                throw new InvalidOperationException("RPC message length out of bounds: " + length);
        }
    }
}
