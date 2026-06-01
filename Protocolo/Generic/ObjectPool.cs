using System;

namespace Protocolo.Framework.Generic
{
    public sealed class ObjectPool<T> : IDisposable where T : class
    {
        private LockFreeQueue<T> m_objects = new LockFreeQueue<T>();
        private Func<T> m_creator;

        public ObjectPool(Func<T> creator, int baseObjectCount = 0)
        {
            m_creator = creator;
            for (int i = 0; i < baseObjectCount; i++)
            {
                m_objects.Enqueue(m_creator());
            }
        }

        public T Pop()
        {
            T obj = null;

            if (!m_objects.TryDequeue(out obj))
            {
                obj = m_creator();
            }

            return obj;
        }

        public void Push(T obj)
        {
            if (obj is IPoolable)
            {
                ((IPoolable)obj).Cleanup();
            }

            m_objects.Enqueue(obj);
        }

        public void Dispose()
        {
            while (m_objects.Count > 0)
            {
                var obj = Pop();
                if (obj is IPoolable)
                {
                    ((IPoolable)obj).Cleanup();
                }

                if (obj is IDisposable)
                {
                    ((IDisposable)obj).Dispose();
                }
            }
        }
    }
}
