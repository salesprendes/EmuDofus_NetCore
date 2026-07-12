using System;
using System.Buffers;
using System.Threading;

namespace Protocolo.Framework.Network
{
    public sealed class BufferManager : IDisposable
    {
        private readonly ArrayPool<byte> m_bufferPool;
        private readonly int m_bufferSize;
        private readonly int m_maxBuffers;
        private int m_leasedBuffers;
        private int m_disposed;

        /// <param name="maxBuffers">Cero permite crecer sin límite lógico.</param>
        public BufferManager(int bufferSize, int maxBuffers = 0)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (maxBuffers < 0)
                throw new ArgumentOutOfRangeException(nameof(maxBuffers));

            m_bufferSize = bufferSize;
            m_maxBuffers = maxBuffers;
            m_bufferPool = ArrayPool<byte>.Shared;
        }

        public void SetBuffer(IBufferHandler bufferHandler)
        {
            if (bufferHandler == null)
                throw new ArgumentNullException(nameof(bufferHandler));

            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_disposed) != 0, this);

            if (Interlocked.Increment(ref m_leasedBuffers) > m_maxBuffers && m_maxBuffers > 0)
            {
                Interlocked.Decrement(ref m_leasedBuffers);
                throw new InvalidOperationException("No more free offset on this BufferManager.");
            }

            byte[] buffer = null;
            try
            {
                buffer = m_bufferPool.Rent(m_bufferSize);
                bufferHandler.SetBuffer(buffer, 0, m_bufferSize);
            }
            catch
            {
                if (buffer != null)
                    m_bufferPool.Return(buffer);

                Interlocked.Decrement(ref m_leasedBuffers);
                throw;
            }
        }

        public void FreeBuffer(IBufferHandler bufferHandler)
        {
            if (bufferHandler == null)
                throw new ArgumentNullException(nameof(bufferHandler));

            var buffer = bufferHandler.Buffer;
            if (buffer == null)
                return;

            bufferHandler.SetBuffer(null, 0, 0);
            m_bufferPool.Return(buffer);
            Interlocked.Decrement(ref m_leasedBuffers);
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref m_disposed, 1);
        }
    }
}
