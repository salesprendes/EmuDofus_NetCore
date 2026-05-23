using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Protocolo.Framework.Generic
{
    public static class ConsoleShutdownHandler
    {
        private static ConsoleCtrlDelegate m_windowsHandler;
        private static int m_shutdownRequested;

        public static void Register(Action shutdown)
        {
            if (shutdown == null)
                throw new ArgumentNullException(nameof(shutdown));

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                RequestShutdown(shutdown);
            };

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;

            m_windowsHandler = _ =>
            {
                RequestShutdown(shutdown);
                return true;
            };

            SetConsoleCtrlHandler(m_windowsHandler, true);
        }

        private static void RequestShutdown(Action shutdown)
        {
            if (Interlocked.Exchange(ref m_shutdownRequested, 1) != 0)
                return;

            shutdown();
        }

        private delegate bool ConsoleCtrlDelegate(ConsoleCtrlType controlType);

        private enum ConsoleCtrlType
        {
            CtrlC = 0,
            CtrlBreak = 1,
            CtrlClose = 2,
            CtrlLogoff = 5,
            CtrlShutdown = 6
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handler, bool add);
    }
}
