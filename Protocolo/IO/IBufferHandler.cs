namespace Protocolo.Framework.Network
{
    public interface IBufferHandler
    {
        byte[] Buffer
        {
            get;
        }

        int Offset
        {
            get;
        }

        void SetBuffer(byte[] buffer, int offset, int count);
    }
}
