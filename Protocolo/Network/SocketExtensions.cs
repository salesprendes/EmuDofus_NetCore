using System.Buffers.Binary;
using System;
using System.Linq;
using System.Net;
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

        internal static IPAddress ResolveIPv4Address(string host)
        {
            if (IPAddress.TryParse(host, out var address))
                return address;

            var resolved = Dns.GetHostAddresses(host).FirstOrDefault(entry => entry.AddressFamily == AddressFamily.InterNetwork);
            if (resolved == null)
                throw new SocketException((int)SocketError.HostNotFound);

            return resolved;
        }

        internal static void SetAggressiveKeepAlive(this Socket socket)
        {
            if (!OperatingSystem.IsWindows())
                return;

            try
            {
                var inValue = new byte[12];
                BinaryPrimitives.WriteUInt32LittleEndian(inValue.AsSpan(0, 4), 1u);
                BinaryPrimitives.WriteUInt32LittleEndian(inValue.AsSpan(4, 4), 60000u);
                BinaryPrimitives.WriteUInt32LittleEndian(inValue.AsSpan(8, 4), 10000u);
                socket.IOControl(IOControlCode.KeepAliveValues, inValue, null);
            }
            catch { }
        }
    }
}
