namespace Protocolo.Framework.Network
{
    public interface IServer<TClient>
    {
        void Send(TClient client, byte[] data);
        void Disconnect(TClient client);
    }
}
