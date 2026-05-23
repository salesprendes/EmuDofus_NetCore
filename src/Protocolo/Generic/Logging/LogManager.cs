using System;

namespace Protocolo.Framework.Generic.Logging
{
    public static class LogManager
    {
        public static ILogger GetLogger(Type type) => new ServerLogger(type.Name);
        public static ILogger GetLogger(string name) => new ServerLogger(name);
    }
}
