using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace Pixagen.Ecs.Collections
{
    public sealed class PooledList<T> : IReadCollection, IDisposable
    {
        private PoolAdapter<T> _adapter;
        private T[] _array;
        private int _count;

        public int Count => _count;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _array[index] = value;
        }

        internal PooledList(PoolAdapter<T> adapter)
        {
            _adapter = adapter;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount()
        {
            return _count;
        }

        /// <summary>
        /// Only for debug (use Boxing)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetItem(int index)
        {
            return _array[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Rent(int capacity)
        {
            _array = ArrayPool<T>.Shared.Rent(Math.Max(1, capacity));
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return()
        {
            if (_array == null)
            {
                return;
            }

            _adapter.Return(this);
            ArrayPool<T>.Shared.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _array = null;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetItemRef(int index)
        {
            return ref _array[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            if (_count >= _array.Length)
            {
                Resize(_array.Length < 1024 ? _array.Length * 2 : _array.Length + 1024);
            }

            _array[_count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(in PooledList<T> list)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            int n = list._count;
            if (n <= 0)
            {
                return;
            }

            TryResize(_count + n);
            Array.Copy(list._array, 0, _array, _count, n);
            _count += n;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(IList<T> list)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            int n = list.Count;
            if (n <= 0) return;

            TryResize(_count + n);
            for (int i = 0; i < n; i++)
            {
                _array[_count++] = list[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Clear(true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(bool fast)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            if (!fast && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_array, 0, _count);
            }
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            Array.Copy(_array, 0, array, arrayIndex, _count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            int idx = IndexOf(item);
            if (idx < 0)
            {
                return false;
            }

            RemoveAt(idx);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            return Array.IndexOf(_array, item, 0, _count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            if (_count >= _array.Length)
            {
                Resize(_array.Length < 1024 ? _array.Length * 2 : _array.Length + 1024);
            }

            if (index < _count)
            {
                Array.Copy(_array, index, _array, index + 1, _count - index);
            }

            _array[index] = item;
            _count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            _count--;
            if (index < _count)
            {
                Array.Copy(_array, index + 1, _array, index, _count - index);
            }
            _array[_count] = default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SmartRemoveAt(int index)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            _count--;
            if (index < _count)
            {
                _array[index] = _array[_count];
            }
            _array[_count] = default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledList<T> Copy()
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");

            }
            var list = _adapter.Rent(_count);
            list.AddRange(this);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledList<T> Create(int capacity)
        {
            return _adapter.Rent(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sort()
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            if (_count <= 1)
            {
                return;
            }

            Array.Sort(_array, 0, _count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sort(IComparer<T> comparer)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            if (_count <= 1)
            {
                return;
            }

            Array.Sort(_array, 0, _count, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sort(Comparison<T> comparison)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            if (_count <= 1)
            {
                return;
            }

            Array.Sort(_array, 0, _count, Comparer<T>.Create(comparison));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sort(int index, int length, IComparer<T> comparer)
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            if (length <= 1)
            {
                return;
            }

            Array.Sort(_array, index, length, comparer);
        }

        public T[] ToArray()
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            var r = new T[_count];
            if (_count > 0)
            {
                Array.Copy(_array, r, _count);
            }
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            if (_array == null)
            {
                throw new Exception("Array is not rent on pool");
            }

            return new Enumerator(this);
        }

        /// <summary>
        /// If you want clear list must be use Return. Method Dispose remove list from pool.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _adapter.Dispose(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DisposeInternal()
        {
            if (_array != null)
            {
                ArrayPool<T>.Shared.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                _array = null;
            }
            _adapter = null;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryResize(int min)
        {
            if (_array.Length >= min)
            {
                return;
            }

            int newCap = _array.Length;
            while (newCap < min)
            {
                newCap = newCap < 1024 ? newCap * 2 : newCap + 1024;
            }
            Resize(newCap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int newSize)
        {
            var newArr = ArrayPool<T>.Shared.Rent(newSize);
            Array.Copy(_array, newArr, _count);
            ArrayPool<T>.Shared.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _array = newArr;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _arr;
            private readonly int _len;
            private int _idx;

            internal Enumerator(PooledList<T> list)
            {
                _arr = list._array;
                _len = list._count;
                _idx = -1;
            }

            public T Current =>
                _arr[_idx];

            object IEnumerator.Current =>
                Current!;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() =>
                ++_idx < _len;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() =>
                _idx = -1;

            public void Dispose() { }
        }
    }
}
