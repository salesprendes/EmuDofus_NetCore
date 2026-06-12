using System;
using System.Collections.Generic;
using System.Threading;

namespace Protocolo.Framework.Generic
{
    internal sealed class SingleLinkNode<T>
    {
        public SingleLinkNode<T> Next;
        public T Item;
    }

    public sealed class LockFreeQueue<T> : IEnumerable<T>
    {
        private SingleLinkNode<T> m_head;
        private SingleLinkNode<T> m_tail;
        private int m_count;

        public LockFreeQueue()
        {
            m_head = new SingleLinkNode<T>();
            m_tail = m_head;
        }

        public LockFreeQueue(IEnumerable<T> items) : this()
        {
            foreach (var item in items)
            {
                Enqueue(item);
            }
        }

        public int Count
        {
            get { return Volatile.Read(ref m_count); }
        }

        public void Enqueue(T item)
        {
            var newNode = new SingleLinkNode<T> { Item = item };
            var spin = new SpinWait();

            while (true)
            {
                SingleLinkNode<T> oldTail = Volatile.Read(ref m_tail);
                SingleLinkNode<T> oldTailNext = Volatile.Read(ref oldTail.Next);

                if (oldTail == Volatile.Read(ref m_tail))
                {
                    if (oldTailNext == null)
                    {
                        if (Interlocked.CompareExchange(ref oldTail.Next, newNode, null) == null)
                        {
                            Interlocked.CompareExchange(ref m_tail, newNode, oldTail);
                            Interlocked.Increment(ref m_count);
                            return;
                        }
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref m_tail, oldTailNext, oldTail);
                    }
                }

                spin.SpinOnce();
            }
        }

        public T TryDequeue()
        {
            TryDequeue(out T item);
            return item;
        }

        public bool TryDequeue(out T item)
        {
            var spin = new SpinWait();

            while (true)
            {
                SingleLinkNode<T> oldHead = Volatile.Read(ref m_head);
                SingleLinkNode<T> oldTail = Volatile.Read(ref m_tail);
                SingleLinkNode<T> oldHeadNext = Volatile.Read(ref oldHead.Next);

                if (oldHead == Volatile.Read(ref m_head))
                {
                    if (oldHead == oldTail)
                    {
                        if (oldHeadNext == null)
                        {
                            item = default;
                            return false;
                        }
                        Interlocked.CompareExchange(ref m_tail, oldHeadNext, oldTail);
                    }
                    else
                    {
                        T value = oldHeadNext.Item;
                        if (Interlocked.CompareExchange(ref m_head, oldHeadNext, oldHead) == oldHead)
                        {
                            Interlocked.Decrement(ref m_count);
                            item = value;
                            return true;
                        }
                    }
                }

                spin.SpinOnce();
            }
        }

        public T Dequeue()
        {
            if (!TryDequeue(out T result))
            {
                throw new InvalidOperationException("the queue is empty");
            }

            return result;
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            SingleLinkNode<T> current = Volatile.Read(ref Volatile.Read(ref m_head).Next);

            while (current != null)
            {
                yield return current.Item;
                current = Volatile.Read(ref current.Next);
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public void Clear()
        {
            var sentinel = new SingleLinkNode<T>();
            Volatile.Write(ref m_tail, sentinel);
            Volatile.Write(ref m_head, sentinel);
            Volatile.Write(ref m_count, 0);
        }
    }
}
