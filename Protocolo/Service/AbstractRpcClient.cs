using Protocolo.Framework.IO;
using Protocolo.Framework.Network;
using System.Collections.Generic;

namespace Protocolo.RPC.Service
{
    public abstract class AbstractRpcClient<TClient> : AbstractTcpClient<TClient> where TClient : AbstractRpcClient<TClient>, new()
    {
        private int m_messageId;
        private int m_messageLength;
        private readonly BinaryQueue m_messageData;

        protected virtual int MaxMessageLength => 1024 * 1024;

        public RpcMessageBuilder MessageBuilder
        {
            get;
            set;
        }

        protected AbstractRpcClient()
        {
            m_messageId = -1;
            m_messageLength = -1;
            m_messageData = new BinaryQueue();
        }

        public IEnumerable<AbstractRcpMessage> GetMessages(byte[] buffer, int offset, int length)
        {
            m_messageData.WriteBytes(buffer, offset, length);

            while (RpcFraming.TryReadMessage(m_messageData, MessageBuilder, MaxMessageLength, ref m_messageLength, ref m_messageId, out var message))
                yield return message;
        }

        public void Send(AbstractRcpMessage message)
        {
            message.Reset();
            message.Serialize();
            base.Send(message.Data);
        }

    }
}
