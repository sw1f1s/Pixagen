using System;
using System.Runtime.CompilerServices;
namespace Pixagen.Ecs.Collections
{
    public struct SparseArrayInt : IReadCollection, IDisposable
    {
        private int[] _denseValues;
        private uint[] _denseSparseIndices;
        private uint[] _sparseItems;
        private uint _denseItemsCount;
        private bool _isDisposed;

        internal int[] DenseValues => _denseValues;
        public uint Count => _denseItemsCount;
        public int Length => _denseValues.Length;

        public SparseArrayInt(uint capacity)
        {
            _denseValues = new int[capacity];
            _denseSparseIndices = new uint[capacity];
            _sparseItems = new uint[capacity];
            _denseItemsCount = 0;
            _isDisposed = false;
        }

        public SparseArrayInt(in SparseArrayInt copy)
        {
            _denseValues = new int[copy._denseValues.Length];
            _denseSparseIndices = new uint[copy._denseSparseIndices.Length];
            _sparseItems = new uint[copy._sparseItems.Length];
            _denseItemsCount = copy._denseItemsCount;
            Array.Copy(copy._denseValues, _denseValues, copy._denseValues.Length);
            Array.Copy(copy._denseSparseIndices, _denseSparseIndices, copy._denseSparseIndices.Length);
            Array.Copy(copy._sparseItems, _sparseItems, copy._sparseItems.Length);
            _isDisposed = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount()
        {
            return (int)_denseItemsCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetItem(int index)
        {
            return _denseValues[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in int item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            TryResize(item);
            _denseValues[_denseItemsCount] = item;
            _denseSparseIndices[_denseItemsCount] = (uint)item;
            _sparseItems[item] = ++_denseItemsCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Replace(in int item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            if (item >= 0 && item < _sparseItems.Length && _sparseItems[item] != 0)
            {
                _denseValues[_sparseItems[item] - 1] = item;
            }
            else
            {
                Add(in item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Has(in int item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            return item >= 0 && item < _sparseItems.Length && _sparseItems[item] != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref int Get(in int item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            uint denseIndex = _sparseItems[item] - 1;
            return ref _denseValues[denseIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref int GetFirst()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            if (_denseItemsCount == 0)
            {
                throw new IndexOutOfRangeException();
            }

            return ref _denseValues[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref int GetLast()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            if (_denseItemsCount == 0)
            {
                throw new IndexOutOfRangeException();
            }

            return ref _denseValues[_denseItemsCount - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(in int item)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            uint denseIndex = _sparseItems[item] - 1;
            _sparseItems[item] = 0;

            _denseItemsCount--;
            uint lastIndex = _denseItemsCount;
            if (lastIndex > denseIndex)
            {
                _denseValues[denseIndex] = _denseValues[lastIndex];
                _denseSparseIndices[denseIndex] = _denseSparseIndices[lastIndex];
                _sparseItems[_denseSparseIndices[denseIndex]] = denseIndex + 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            for (int i = 0; i < _denseItemsCount; i++)
            {
                uint sparseIndex = _denseSparseIndices[i];
                _sparseItems[sparseIndex] = 0;
                _denseValues[i] = 0;
            }
            _denseItemsCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastClear()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }
            _denseItemsCount = 0;
        }

        public Enumerator GetEnumerator()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
            }

            return new Enumerator(in this);
        }

        public override string ToString()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SparseArrayInt));
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
            while (_denseItemsCount >= _denseValues.Length || id >= _sparseItems.Length)
            {
                Resize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize()
        {
            Array.Resize(ref _denseValues, _denseValues.Length * 2);
            Array.Resize(ref _denseSparseIndices, _denseSparseIndices.Length * 2);
            Array.Resize(ref _sparseItems, _sparseItems.Length * 2);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _denseValues = null;
            _denseSparseIndices = null;
            _sparseItems = null;
            _denseItemsCount = 0;
        }

        public struct Enumerator : IDisposable
        {
            private SparseArrayInt _data;
            private uint _count;
            private int _idx;

            internal Enumerator(in SparseArrayInt data)
            {
                _data = data;
                _count = data.Count;
                _idx = -1;
            }

            public ref int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _data._denseValues[_idx];
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
