using System;
using System.Runtime.CompilerServices;
namespace Pixagen.Ecs.Collections
{
    public struct SparseArray<T> : IReadCollection, IDisposable
    {
        private Entry[] _denseItems;
        private uint[] _sparseItems;
        private uint _denseItemsCount;
        private bool _isDisposed;

        internal Entry[] DenseItems => _denseItems;
        public uint Count => _denseItemsCount;
        public int Length => _denseItems.Length;

        public SparseArray(uint capacity)
        {
            _denseItems = new Entry[capacity];
            _sparseItems = new uint[capacity];
            _denseItemsCount = 0;
            _isDisposed = false;
        }

        public SparseArray(in SparseArray<T> copy)
        {
            _denseItems = new Entry[copy._denseItems.Length];
            _sparseItems = new uint[copy._sparseItems.Length];
            _denseItemsCount = copy._denseItemsCount;
            Array.Copy(copy._denseItems, _denseItems, copy._denseItems.Length);
            Array.Copy(copy._sparseItems, _sparseItems, copy._sparseItems.Length);
            _isDisposed = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount()
        {
            return (int)_denseItemsCount;
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

            TryResize(id);
            _denseItems[_denseItemsCount] = new Entry((uint)id, item);
            _sparseItems[id] = ++_denseItemsCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Replace(int id, in T item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            if (id >= 0 && id < _sparseItems.Length && _sparseItems[id] != 0)
            {
                _denseItems[_sparseItems[id] - 1].Value = item;
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

            return id >= 0 && id < _sparseItems.Length && _sparseItems[id] != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int id)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            uint denseIndex = _sparseItems[id] - 1;
            return ref _denseItems[denseIndex].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetFirst()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            if (_denseItemsCount == 0)
            {
                throw new IndexOutOfRangeException();
            }

            return ref _denseItems[0].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetLast()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            if (_denseItemsCount == 0)
            {
                throw new IndexOutOfRangeException();
            }

            return ref _denseItems[_denseItemsCount - 1].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int id)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }

            uint denseIndex = _sparseItems[id] - 1;
            _sparseItems[id] = 0;

            _denseItemsCount--;
            uint lastIndex = _denseItemsCount;
            if (lastIndex > denseIndex)
            {
                _denseItems[denseIndex] = _denseItems[lastIndex];
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

            for (int i = 0; i < _denseItemsCount; i++)
            {
                uint sparseIndex = _denseItems[i].Index;
                _sparseItems[sparseIndex] = 0;
                _denseItems[i].Value = default;
            }
            _denseItemsCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastClear()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArray<T>));
            }
            _denseItemsCount = 0;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryResize(int id)
        {
            while (_denseItemsCount >= _denseItems.Length || id >= _sparseItems.Length)
            {
                Resize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize()
        {
            Array.Resize(ref _denseItems, _denseItems.Length * 2);
            Array.Resize(ref _sparseItems, _sparseItems.Length * 2);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _denseItems = null;
            _sparseItems = null;
            _denseItemsCount = 0;
        }

        internal struct Entry
        {
            public uint Index;
            public T Value;

            public Entry(uint index, T value)
            {
                Index = index;
                Value = value;
            }
        }

        public struct Enumerator<T> : IDisposable
        {
            private SparseArray<T> _data;
            private uint _count;
            private int _idx;

            internal Enumerator(in SparseArray<T> data)
            {
                _data = data;
                _count = data.Count;
                _idx = -1;
            }

            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _data._denseItems[_idx].Value;
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