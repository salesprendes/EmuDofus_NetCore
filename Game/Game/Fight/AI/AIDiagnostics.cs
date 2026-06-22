using Protocolo.Framework.Generic.Logging;
using System;

namespace Game.Fight.AI
{
    // Registro centralizado de las excepciones que la IA captura y de las que se recupera con un
    // valor por defecto (pathfinding, linea de vision, zonas...). Solo emite bajo LOG_DEBUG para no
    // ensuciar el log en produccion, pero permite descubrir fallos reales que, de otro modo,
    // quedarian completamente silenciados por los bloques catch.
    internal static class AIDiagnostics
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(AIDiagnostics));

        public static void LogSwallowed(string where, Exception ex)
        {
            if (WorldConfig.LOG_DEBUG)
                Logger.Debug($"[IA] Excepcion controlada en {where}: {ex}");
        }
    }
}
