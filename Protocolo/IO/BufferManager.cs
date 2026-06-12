using System;
using System.Collections.Concurrent;

namespace Protocolo.Framework.Network
{
    public sealed class BufferManager : IDisposable
    {
        private byte[] m_bufferBlock;
        private readonly int m_bufferSize;
        private readonly ConcurrentStack<int> m_freeOffset;

        public BufferManager(int bufferSize, int chunkCount)
        {
            m_bufferSize = bufferSize;
            m_freeOffset = new ConcurrentStack<int>();
            m_bufferBlock = new byte[bufferSize * chunkCount];
            for (int i = 0; i < chunkCount; i++)
                m_freeOffset.Push(bufferSize * i);
        }

        public void SetBuffer(IBufferHandler bufferHandler)
        {
            int offset = -1;
            if (m_freeOffset.TryPop(out offset))
            {
                bufferHandler.SetBuffer(m_bufferBlock, offset, m_bufferSize);
            }
            else
            {
                throw new InvalidOperationException("No more free offset on this BufferManager.");
            }
        }

        public void FreeBuffer(IBufferHandler bufferHandler)
        {
            if (bufferHandler == null)
                throw new ArgumentNullException(nameof(bufferHandler));

            if (m_bufferBlock == null)
                return;

            m_freeOffset.Push(bufferHandler.Offset);
            bufferHandler.SetBuffer(null, 0, 0);
        }

        public void Dispose()
        {
            m_bufferBlock = null;
            m_freeOffset.Clear();
        }
    }
}
