using System.Net.Sockets;

namespace Protocolo.Framework.Network
{
    internal static class SocketExtensions
    {
        internal static void SafeDispose(this Socket socket)
        {
            try { socket.Shutdown(SocketShutdown.Both); } catch { }
            try { socket.Close(); } catch { }
        }

        internal static void ConfigureBase(this Socket socket)
        {
            socket.NoDelay = true;
            socket.Blocking = false;
            socket.LingerState = new LingerOption(false, 0);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }
    }
}
