using Protocolo.Framework.Generic;
using System;

namespace Protocolo.Framework.Network
{
    public interface IFrame<TClient, TMessage>
    {
        bool Process(TClient client, TMessage message);
    }

    public abstract class AbstractNetworkFrame<TFrame, TClient, TMessage> : Singleton<TFrame>, IFrame<TClient, TMessage> where TFrame : AbstractNetworkFrame<TFrame, TClient, TMessage>, new()
    {
        public abstract Action<TClient, TMessage> GetHandler(TMessage message);

        public bool Process(TClient client, TMessage message)
        {
            var handler = GetHandler(message);
            if (handler != null)
            {
                try
                {
                    handler(client, message);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error en el manejador del frame: {ex}");
                }
                return true;
            }
            return false;
        }
    }
}
