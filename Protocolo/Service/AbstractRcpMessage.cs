using Protocolo.Framework.IO;
using System;
using System.Buffers.Binary;

namespace Protocolo.RPC.Service
{
    public abstract class AbstractRcpMessage : BinaryQueue
    {
        private byte[] m_cache;

        public byte[] Data
        {
            get
            {
                if (m_cache == null)
                {
                    var dataLength = Count;

                    m_cache = new byte[8 + dataLength];
                    BinaryPrimitives.WriteInt32LittleEndian(m_cache.AsSpan(0, 4), dataLength);
                    BinaryPrimitives.WriteInt32LittleEndian(m_cache.AsSpan(4, 4), Id);
                    CopyTo(m_cache, 8, dataLength);
                }

                return m_cache;
            }
        }

        public abstract int Id
        {
            get;
        }

        public void Reset()
        {
            Clear();
            m_cache = null;
        }

        public void SetData(byte[] data)
        {
            Reset();
            WriteBytes(data);
        }

        public void SetData(BinaryQueue data, int length)
        {
            Reset();
            WriteBytes(data, length);
        }

        public abstract void Deserialize();
        public abstract void Serialize();
    }
}
