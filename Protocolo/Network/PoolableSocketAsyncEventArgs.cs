using System;
using System.Net.Sockets;
using System.Threading;

namespace Protocolo.Framework.Network
{
    public sealed class PoolableSocketAsyncEventArgs : SocketAsyncEventArgs, IBufferHandler, IDisposable
    {
        private BufferManager m_buffManager;
        private int m_disposed;
        private int m_poolState;

        public PoolableSocketAsyncEventArgs(BufferManager bufferManager = null)
        {
            m_buffManager = bufferManager;
            m_buffManager?.SetBuffer(this);
        }

        internal bool TryRent()
        {
            return Interlocked.CompareExchange(ref m_poolState, 0, 1) == 1;
        }

        internal bool TryReturn()
        {
            return Volatile.Read(ref m_disposed) == 0 && Interlocked.CompareExchange(ref m_poolState, 1, 0) == 0;
        }

        public new void Dispose()
        {
            if (Interlocked.Exchange(ref m_disposed, 1) != 0)
                return;

            Interlocked.Exchange(ref m_poolState, 2);

            if (m_buffManager != null)
            {
                m_buffManager.FreeBuffer(this);
                m_buffManager = null;
            }

            base.Dispose();
        }
    }
}
