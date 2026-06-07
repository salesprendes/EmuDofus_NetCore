using System;
using System.Collections.Concurrent;

namespace Protocolo.Framework.Generic.Logging
{
    public static class LogManager
    {
        private static readonly ConcurrentDictionary<string, ILogger> Loggers = new ConcurrentDictionary<string, ILogger>();
        public static ILogger GetLogger(Type type) => GetLogger(type == null ? "Unknown" : type.Name);
        public static ILogger GetLogger(string name) => Loggers.GetOrAdd(string.IsNullOrWhiteSpace(name) ? "Unknown" : name, static loggerName => new ServerLogger(loggerName));
        public static void ConfigureLevels(string minimumLevel, string consoleLevel = null, string fileLevel = null) => ServerLogger.ConfigureLevels(minimumLevel, consoleLevel, fileLevel);
        public static void Flush() => ServerLogger.Flush();
        public static void Shutdown() => ServerLogger.Shutdown();
    }
}
