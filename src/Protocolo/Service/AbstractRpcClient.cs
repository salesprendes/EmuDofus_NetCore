using System;
using System.Collections.Generic;
using Protocolo.Framework.IO;
using Protocolo.Framework.Network;

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

            while (true)
            {
                if (m_messageLength == -1)
                {
                    if (m_messageData.Count < sizeof(int))
                        yield break;

                    m_messageLength = m_messageData.ReadInt();
                    ValidateMessageLength(m_messageLength);
                }

                if (m_messageId == -1)
                {
                    if (m_messageData.Count < sizeof(int))
                        yield break;

                    m_messageId = m_messageData.ReadInt();
                }

                if (m_messageData.Count < m_messageLength)
                    yield break;

                yield return MessageBuilder.BuildMessage(m_messageId, m_messageData, m_messageLength);

                m_messageId = -1;
                m_messageLength = -1;
            }
        }

        public void Send(AbstractRcpMessage message)
        {
            message.Reset();
            message.Serialize();
            base.Send(message.Data);
        }

        private void ValidateMessageLength(int length)
        {
            if (length <= 0 || length > MaxMessageLength)
                throw new InvalidOperationException("RPC message length out of bounds: " + length);
        }
    }
}
