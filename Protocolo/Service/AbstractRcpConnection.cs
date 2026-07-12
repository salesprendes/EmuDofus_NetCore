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

        protected override void OnConnecting()
        {
            // Descartar el framing a medio leer de la sesión anterior: si la conexión cayó
            // en mitad de un mensaje, los restos desincronizarían la nueva sesión.
            m_messageId = -1;
            m_messageLength = -1;
            m_messageData.Clear();
        }

        protected override void OnBytesRead(byte[] buffer, int offset, int length)
        {
            m_messageData.WriteBytes(buffer, offset, length);

            while (RpcFraming.TryReadMessage(m_messageData, MessageBuilder, MaxMessageLength, ref m_messageLength, ref m_messageId, out var message))
                OnMessageEvent?.Invoke(message);
        }

        protected abstract override void OnConnected();
        protected abstract override void OnDisconnected();
        protected abstract void OnMessage(AbstractRcpMessage message);
    }
}
