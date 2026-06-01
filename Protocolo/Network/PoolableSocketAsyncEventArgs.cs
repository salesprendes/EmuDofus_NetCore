using System;
using System.Net.Sockets;
using System.Threading;

namespace Protocolo.Framework.Network
{
    public sealed class PoolableSocketAsyncEventArgs : SocketAsyncEventArgs, IBufferHandler, IDisposable
    {
        private BufferManager m_buffManager;
        private int m_disposed;

        public PoolableSocketAsyncEventArgs(BufferManager bufferManager)
        {
            m_buffManager = bufferManager;
            m_buffManager.SetBuffer(this);
        }

        public new void Dispose()
        {
            if (Interlocked.Exchange(ref m_disposed, 1) != 0)
                return;

            if (m_buffManager != null)
            {
                m_buffManager.FreeBuffer(this);
                m_buffManager = null;
            }

            base.Dispose();
        }
    }
}
