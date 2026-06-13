using Protocolo.Framework.Generic.Logging;
using Protocolo.Framework.Network;
using System;
using System.Collections.Generic;

namespace Protocolo.RPC.Service
{
    public abstract class AbstractRpcService<TServer, TClient, TMessageBuilder> : AbstractTcpServer<TServer, TClient> where TServer : AbstractRpcService<TServer, TClient, TMessageBuilder>, new() where TClient : AbstractRpcClient<TClient>, new() where TMessageBuilder : RpcMessageBuilder, new()
    {
        public RpcMessageBuilder MessageBuilder
        {
            get;
            private set;
        }

        private readonly Dictionary<int, Action<TClient, AbstractRcpMessage>> m_handlers;

        protected AbstractRpcService()
        {
            m_handlers = new Dictionary<int, Action<TClient, AbstractRcpMessage>>();
            MessageBuilder = new TMessageBuilder();
        }

        public void RegisterHandler(int messageId, Action<TClient, AbstractRcpMessage> handler)
        {
            if (!m_handlers.TryAdd(messageId, handler))
                throw new InvalidOperationException($"RPCService::RegisterHandler ya tiene un manejador registrado para mensajeId={messageId}");
        }

        private void HandleMessage(TClient client, AbstractRcpMessage message)
        {
            if (!m_handlers.TryGetValue(message.Id, out var handler))
            {
                Logger.Debug($"RPCService::HandleMessage manejador no registrado para mensajeId={message.Id}");
            }
            else
                AddMessage(() => handler(client, message));
        }

        protected override void OnClientConnected(TClient client)
        {
            client.MessageBuilder = MessageBuilder;
            AddMessage(() => OnRPCClientConnected(client));
        }

        protected override void OnClientDisconnected(TClient client) => AddMessage(() => OnRPCClientDisconnected(client));

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
                Logger.Warn($"RPCService::OnDataReceived carga invalida desde {client.Ip}: {ex.Message}");
                Disconnect(client);
            }
        }

        protected abstract void OnRPCClientConnected(TClient client);
        protected abstract void OnRPCClientDisconnected(TClient client);
        protected abstract void OnMessageReceived(TClient client, AbstractRcpMessage message);
    }
}
