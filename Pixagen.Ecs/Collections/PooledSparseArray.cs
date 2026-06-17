using System;
using System.Runtime.CompilerServices;
namespace Pixagen.Ecs.Collections
{
    public struct PooledSparseArray<T> : IReadCollection, IDisposable
    {
        private PooledList<Entry> _denseItems;
        private PooledList<int> _sparseItems;
        private bool _isDisposed;

        public int Count => _denseItems.Count;

        public PooledSparseArray(int capacity, IPoolFactory factory)
        {
            _denseItems = factory.Rent<Entry>(capacity);
            _sparseItems = factory.Rent<int>(capacity);
            _isDisposed = false;
        }

        public PooledSparseArray(in PooledSparseArray<T> copy)
        {
            _denseItems = copy._denseItems.Copy();
            _sparseItems = copy._sparseItems.Copy();
            _isDisposed = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount()
        {
            return Count;
        }

        /// <summary>
        /// Only for debug (use Boxing)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetItem(int index)
        {
            return _denseItems[index].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int id, in T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            _denseItems.Add(new Entry(id, item));
            while (_sparseItems.Count <= id)
            {
                _sparseItems.Add(0);
            }
            _sparseItems[id] = _denseItems.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Replace(int id, in T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            if (id >= 0 && id < _sparseItems.Count && _sparseItems[id] != 0)
            {
                _denseItems.GetItemRef(_sparseItems[id] - 1).Value = item;
            }
            else
            {
                Add(id, item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Has(int id)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            return id >= 0 && id < _sparseItems.Count && _sparseItems[id] != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int id)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            int denseIndex = _sparseItems[id] - 1;
            return ref _denseItems.GetItemRef(denseIndex).Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetFirst()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            if (_denseItems.Count == 0)
            {
                throw new IndexOutOfRangeException();
            }

            return ref _denseItems.GetItemRef(0).Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetLast()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            if (_denseItems.Count == 0)
            {
                throw new IndexOutOfRangeException();
            }

            return ref _denseItems.GetItemRef(_denseItems.Count - 1).Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int id)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            int denseIndex = _sparseItems[id] - 1;
            _sparseItems[id] = 0;
            _denseItems.SmartRemoveAt(denseIndex);

            if (_denseItems.Count > denseIndex)
            {
                _sparseItems[_denseItems[denseIndex].Index] = denseIndex + 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            for (int i = _denseItems.Count - 1; i >= 0; i--)
            {
                _sparseItems[_denseItems[i].Index] = 0;
                _denseItems.RemoveAt(i);
            }
        }

        public Enumerator<T> GetEnumerator()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            return new Enumerator<T>(this);
        }

        public override string ToString()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            string s = string.Empty;
            foreach (var value in this)
            {
                s += value + ", ";
            }
            return s;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _denseItems?.Return();
            _sparseItems?.Return();

            _denseItems = null;
            _sparseItems = null;
        }

        internal struct Entry
        {
            public int Index;
            public T Value;

            public Entry(int index, T value)
            {
                Index = index;
                Value = value;
            }
        }

        public struct Enumerator<T> : IDisposable
        {
            private PooledSparseArray<T> _data;
            private int _count;
            private int _idx;

            internal Enumerator(in PooledSparseArray<T> data)
            {
                _data = data;
                _count = data.Count;
                _idx = -1;
            }

            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _data._denseItems.GetItemRef(_idx).Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_idx < _count;
            }

            public void Dispose()
            {
                _data = default;
                _count = 0;
                _idx = 0;
            }
        }
    }
}