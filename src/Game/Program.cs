using Protocolo.Framework.Generic.Logging;
using System;
using System.Runtime;
using System.Threading;

namespace Game.App
{
    public static class Program
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Program));

        private static void Main(string[] args)
        {
            Logger.Info("Iniciando Emulador...");

            InitializeGCServer();
            WorldService.Instance.Start("./config.json");

            ManualResetEventSlim shutdown = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                WorldService.Instance.SaveWorldSync();
                Logger.Info("Apagando servidor...");
                WorldService.Instance.Stop();
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
