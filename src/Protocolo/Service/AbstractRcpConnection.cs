using System;
using Protocolo.Framework.IO;
using Protocolo.Framework.Network;

namespace Protocolo.RPC.Service
{
    public abstract class AbstractRcpConnection<TMessageBuilder> : AbstractSocketClient where TMessageBuilder : RpcMessageBuilder, new()
    {
        private int m_messageId;
        private int m_messageLength;
        private readonly BinaryQueue m_messageData;

        protected virtual int MaxMessageLength => 1024 * 1024;

        public event Action<AbstractRcpMessage> OnMessageEvent;

        public RpcMessageBuilder MessageBuilder
        {
            get;
            private set;
        }

        protected AbstractRcpConnection()
        {
            MessageBuilder = new TMessageBuilder();
            m_messageId = -1;
            m_messageLength = -1;
            m_messageData = new BinaryQueue();

            OnMessageEvent += OnMessage;
        }

        public void Send(AbstractRcpMessage message)
        {
            message.Reset();
            message.Serialize();
            Send(message.Data);
        }

        protected override void OnBytesRead(byte[] buffer, int offset, int length)
        {
            m_messageData.WriteBytes(buffer, offset, length);

            while (true)
            {
                if (m_messageLength == -1)
                {
                    if (m_messageData.Count < sizeof(int))
                        return;

                    m_messageLength = m_messageData.ReadInt();
                    ValidateMessageLength(m_messageLength);
                }

                if (m_messageId == -1)
                {
                    if (m_messageData.Count < sizeof(int))
                        return;

                    m_messageId = m_messageData.ReadInt();
                }

                if (m_messageData.Count < m_messageLength)
                    return;

                var message = MessageBuilder.BuildMessage(m_messageId, m_messageData, m_messageLength);
                OnMessageEvent?.Invoke(message);

                m_messageId = -1;
                m_messageLength = -1;
            }
        }

        protected abstract override void OnConnected();
        protected abstract override void OnDisconnected();
        protected abstract void OnMessage(AbstractRcpMessage message);

        private void ValidateMessageLength(int length)
        {
            if (length <= 0 || length > MaxMessageLength)
                throw new InvalidOperationException("RPC message length out of bounds: " + length);
        }
    }
}
