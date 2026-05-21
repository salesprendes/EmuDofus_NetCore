using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Threading;

namespace Game.App
{
    public static class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        private static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

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
