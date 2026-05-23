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
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // evita que el proceso muera abruptamente
                Logger.Info("Apagando servidor...");
                AuthService.Instance.Stop();
                shutdown.Set();
            };

            shutdown.Wait();
            Logger.Info("Servidor detenido.");
        }

        private static void InitializeGCServer()
        {
            GCSettings.LatencyMode = GCSettings.IsServerGC ? GCLatencyMode.Batch : GCLatencyMode.Interactive;
        }
    }
}
