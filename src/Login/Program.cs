using Protocolo.Framework.Generic;
using Protocolo.Framework.Generic.Logging;
using System;
using System.Runtime;
using System.Threading;

namespace Login.App
{
    public static class Program
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Program));

        private static void Main(string[] args)
        {
            Logger.Info("Iniciando Emulador...");

            InitializeGCServer();
            AuthService.Instance.Start("./config.json");

            var shutdown = new ManualResetEventSlim(false);
            ConsoleShutdownHandler.Register(() =>
            {
                Logger.Info("Apagando servidor...");
                try
                {
                    AuthService.Instance.Stop();
                }
                finally
                {
                    shutdown.Set();
                }
            });

            shutdown.Wait();
            Logger.Info("Servidor detenido.");
        }

        private static void InitializeGCServer()
        {
            GCSettings.LatencyMode = GCSettings.IsServerGC ? GCLatencyMode.Batch : GCLatencyMode.Interactive;
        }
    }
}
