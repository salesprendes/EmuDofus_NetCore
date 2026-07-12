using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Protocolo.Framework.Network
{
    internal sealed class SocketAsyncEventArgsPool : IDisposable
    {
        private readonly ConcurrentBag<PoolableSocketAsyncEventArgs> m_items;
        private readonly Func<PoolableSocketAsyncEventArgs> m_factory;
        private readonly Action<PoolableSocketAsyncEventArgs> m_reset;
        private readonly int m_maxRetained;
        private int m_retainedCount;
        private int m_disposed;

        internal SocketAsyncEventArgsPool(Func<PoolableSocketAsyncEventArgs> factory, Action<PoolableSocketAsyncEventArgs> reset, int initialCount, int maxRetained)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (reset == null)
                throw new ArgumentNullException(nameof(reset));
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount));
            if (maxRetained <= 0 || initialCount > maxRetained)
                throw new ArgumentOutOfRangeException(nameof(maxRetained));

            m_factory = factory;
            m_reset = reset;
            m_maxRetained = maxRetained;
            m_items = new ConcurrentBag<PoolableSocketAsyncEventArgs>();

            for (var i = 0; i < initialCount; i++)
                Return(factory());
        }

        internal PoolableSocketAsyncEventArgs Rent()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_disposed) != 0, this);

            if (m_items.TryTake(out var item))
            {
                Interlocked.Decrement(ref m_retainedCount);
                if (item.TryRent())
                    return item;

                item.Dispose();
            }

            return m_factory();
        }

        internal void Return(PoolableSocketAsyncEventArgs item)
        {
            if (item == null || !item.TryReturn())
                return;
                
            m_reset(item);

            var retained = Interlocked.Increment(ref m_retainedCount);
            if (retained <= m_maxRetained && Volatile.Read(ref m_disposed) == 0)
            {
                m_items.Add(item);

                // Cierra la carrera con Dispose entre la comprobación y el Push.
                if (Volatile.Read(ref m_disposed) != 0 && m_items.TryTake(out var itemAfterDispose))
                {
                    Interlocked.Decrement(ref m_retainedCount);
                    itemAfterDispose.Dispose();
                }
            }
            else
            {
                Interlocked.Decrement(ref m_retainedCount);
                item.Dispose();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_disposed, 1) != 0)
                return;

            while (m_items.TryTake(out var item))
            {
                Interlocked.Decrement(ref m_retainedCount);
                item.Dispose();
            }
        }
    }
}
