using System;

namespace Protocolo.Framework.Generic.Logging
{
    public interface ILogger
    {
        void Debug(object message);
        void Info(object message);
        void Warn(object message, Exception exception = null);
        void Error(object message, Exception exception = null);
        void Fatal(object message, Exception exception = null);
    }
}
