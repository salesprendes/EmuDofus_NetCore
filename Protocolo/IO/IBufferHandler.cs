namespace Protocolo.Framework.Network
{
    public interface IBufferHandler
    {
        int Offset
        {
            get;
        }

        void SetBuffer(byte[] buffer, int offset, int count);
    }
}
