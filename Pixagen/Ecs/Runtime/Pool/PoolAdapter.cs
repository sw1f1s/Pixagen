using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;
namespace Pixagen.Ecs.Runtime {
    internal abstract class AbstractPoolAdapter : IDisposable {
        internal abstract void ReturnAll();
        public abstract void Dispose();
    }
    
    internal sealed class PoolAdapter<T> : AbstractPoolAdapter {
        private const int DEFAULT_CAPACITY = 4;
        private readonly Queue<PooledList<T>> _pool = new Queue<PooledList<T>>();
        private readonly HashSet<PooledList<T>> _active = new HashSet<PooledList<T>>();
        private bool _isDisposed;

        internal PoolAdapter(int initialCapacity = DEFAULT_CAPACITY) {
            Resize(initialCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledList<T> Rent(int initialCapacityList = DEFAULT_CAPACITY) {
            TryResize();
            var list = _pool.Dequeue();
            list.Rent(initialCapacityList);
            _active.Add(list);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Return(PooledList<T> list) {
            _active.Remove(list);
            _pool.Enqueue(list);
            list.Clear(true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void ReturnAll() {
            foreach (var list in _active) {
                list.Clear(true);
                _pool.Enqueue(list);
            }
            
            _active.Clear();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Dispose(PooledList<T> list) {
            _active.Remove(list);
            list.DisposeInternal();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryResize(int initialCapacity = DEFAULT_CAPACITY) {
            if (_pool.Count > 0) {
                return;
            }

            Resize(DEFAULT_CAPACITY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int initialCapacity = DEFAULT_CAPACITY) {
            for (int i = 0; i < initialCapacity; i++) {
                _pool.Enqueue(new PooledList<T>(this));
            }
        }

        public override void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;
            foreach (var pool in _active) {
                pool.DisposeInternal();
            }
            
            foreach (var pool in _pool) {
                pool.DisposeInternal();
            }
            
            _pool.Clear();
            _active.Clear();
        }
    }
}