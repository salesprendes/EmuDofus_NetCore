using System;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Threading;
using log4net;
using log4net.Config;

namespace Login.App
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
