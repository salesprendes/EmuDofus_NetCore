using System;
using System.Collections.Generic;
using Protocolo.Framework.Network;

namespace Protocolo.RPC.Service
{
    public abstract class AbstractRpcService<TServer, TClient, TMessageBuilder> : AbstractTcpServer<TServer, TClient>
        where TServer : AbstractRpcService<TServer, TClient, TMessageBuilder>, new()
        where TClient : AbstractRpcClient<TClient>, new()
        where TMessageBuilder : RpcMessageBuilder, new ()
    {
        /// <summary>
        /// 
        /// </summary>
        public RpcMessageBuilder MessageBuilder
        {
            get;
            private set;
        }

        private Dictionary<int, Action<TClient, AbstractRcpMessage>> m_handlers;

        protected AbstractRpcService()
        {
            m_handlers = new Dictionary<int, Action<TClient, AbstractRcpMessage>>();
            MessageBuilder = new TMessageBuilder();
        }

        public void RegisterHandler(int messageId, Action<TClient, AbstractRcpMessage> handler)
        {
            if (m_handlers.ContainsKey(messageId))
                throw new InvalidOperationException(string.Format("RPCService::RegisterHandler already registered handler for messageId = {0}", messageId));
            else
                m_handlers.Add(messageId, handler);
        }

        private void HandleMessage(TClient client, AbstractRcpMessage message)
        {
            if (!m_handlers.TryGetValue(message.Id, out var handler))
            {
                Logger.Debug(string.Format("RPCService::HandlerMessage unregistered handler for messageId={0}", message.Id));
            }
            else
                AddMessage(() => handler(client, message));
        }

        protected override void OnClientConnected(TClient client)
        {
            client.MessageBuilder = MessageBuilder;

            // execute in thread context
            AddMessage(() => OnRPCClientConnected(client));
        }

        protected override void OnClientDisconnected(TClient client)
        {
            // execute in thread context
            AddMessage(() => OnRPCClientDisconnected(client));
        }

        protected override void OnDataReceived(TClient client, byte[] buffer, int offset, int count)
        {
            try
            {
                foreach (var message in client.GetMessages(buffer, offset, count))
                {
                    AddMessage(() => OnMessageReceived(client, message));
                    HandleMessage(client, message);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(string.Format("RPCService::OnDataReceived invalid payload from {0}: {1}", client.Ip, ex.Message));
                Disconnect(client);
            }
        }

        protected abstract void OnRPCClientConnected(TClient client);
        protected abstract void OnRPCClientDisconnected(TClient client);
        protected abstract void OnMessageReceived(TClient client, AbstractRcpMessage message);
    }
}
