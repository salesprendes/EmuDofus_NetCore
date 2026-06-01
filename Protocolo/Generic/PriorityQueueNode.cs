using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Protocolo.Framework.Generic
{
    public sealed class PriorityQueue<T> : IEnumerable<T> where T : IComparable<T>
    {
        private T[] _heap;
        private int _count;
        private readonly IComparer<T> _comparer;

        public int Count => _count;
        public IComparer<T> Comparer => _comparer;

        public PriorityQueue() : this(16, null) { }
        public PriorityQueue(int capacity) : this(capacity, null) { }
        public PriorityQueue(IComparer<T> comparer) : this(16, comparer) { }

        public PriorityQueue(int capacity, IComparer<T> comparer)
        {
            _heap = new T[capacity > 0 ? capacity : 16];
            _comparer = comparer ?? Comparer<T>.Default;
        }

        public PriorityQueue(IEnumerable<T> source, IComparer<T> comparer = null)
        {
            _comparer = comparer ?? Comparer<T>.Default;
            var list = new List<T>(source);
            _heap = list.ToArray();
            _count = _heap.Length;
            for (int i = (_count >> 1) - 1; i >= 0; i--)
                SiftDown(i);
        }

        // Indexed access into the underlying heap array — same semantics as original base[index].
        public T this[int index]
        {
            get => _heap[index];
            set { _heap[index] = value; EnsureHeapCondition(index); }
        }

        public void Enqueue(T item) => Add(item);

        public void Add(T item)
        {
            if (_count == _heap.Length) Grow();
            _heap[_count] = item;
            SiftUp(_count);
            _count++;
        }

        public T Dequeue()
        {
            if (_count == 0) throw new InvalidOperationException("Queue is empty.");
            var result = _heap[0];
            RemoveAt(0);
            return result;
        }

        public T Peek()
        {
            if (_count == 0) throw new InvalidOperationException("Queue is empty.");
            return _heap[0];
        }

        public bool TryDequeue(out T item)
        {
            if (_count == 0) { item = default; return false; }
            item = Dequeue();
            return true;
        }

        public bool TryPeek(out T item)
        {
            if (_count == 0) { item = default; return false; }
            item = _heap[0];
            return true;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _count--;
            if (index < _count)
            {
                _heap[index] = _heap[_count];
                EnsureHeapCondition(index);
            }
            _heap[_count] = default;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
                if (comparer.Equals(_heap[i], item)) return i;
            return -1;
        }

        public void Clear()
        {
            Array.Clear(_heap, 0, _count);
            _count = 0;
        }

        public void EnsureHeapCondition(int index)
        {
            int moved = SiftUp(index);
            if (moved == index) SiftDown(index);
        }

        public void EnsureHeapCondition()
        {
            for (int i = (_count >> 1) - 1; i >= 0; i--)
                SiftDown(i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SiftUp(int index)
        {
            var item = _heap[index];
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (_comparer.Compare(item, _heap[parent]) >= 0) break;
                _heap[index] = _heap[parent];
                index = parent;
            }
            _heap[index] = item;
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SiftDown(int index)
        {
            var item = _heap[index];
            int lastParent = (_count - 1) >> 1;
            while (index <= lastParent)
            {
                int child = (index << 1) + 1;
                if (child < _count - 1 && _comparer.Compare(_heap[child + 1], _heap[child]) < 0)
                    child++;
                if (_comparer.Compare(item, _heap[child]) <= 0) break;
                _heap[index] = _heap[child];
                index = child;
            }
            _heap[index] = item;
        }

        private void Grow()
        {
            Array.Resize(ref _heap, _heap.Length == 0 ? 4 : _heap.Length * 2);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _heap[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
