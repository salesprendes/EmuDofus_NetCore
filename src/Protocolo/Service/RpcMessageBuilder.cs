using System;
using System.Collections.Generic;
using Protocolo.Framework.IO;

namespace Protocolo.RPC.Service
{
    public abstract class RpcMessageBuilder
    {
        private readonly Dictionary<int, Func<AbstractRcpMessage>> m_messageById;
        protected RpcMessageBuilder()
        {
            m_messageById = new Dictionary<int, Func<AbstractRcpMessage>>();
        }

        public void Register<T>(int messageId)
            where T : AbstractRcpMessage, new()
        {
            m_messageById.Add(messageId, () => new T());
        }
                
        public AbstractRcpMessage BuildMessage(int messageId, BinaryQueue data, int length)
        {
            if (!m_messageById.TryGetValue(messageId, out var factory))
                throw new NotImplementedException(string.Format("RPCMessageBuilder::BuildMessage unknow messageId : {0}", messageId));

            var message = factory();
            message.SetData(data, length);
            message.Deserialize();
            return message;
        }
    }
}
