using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;
namespace Pixagen.Ecs.Runtime {
    internal sealed class PoolFactory : IPoolFactory, IDisposable {
        private readonly Dictionary<Type, AbstractPoolAdapter> _poolAdapters = new Dictionary<Type, AbstractPoolAdapter>();
        private bool _isDisposed;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledList<T> Rent<T>(int initialCapacity = 4) {
            var adapter = GetOrCreateAdapter<T>();
            return adapter.Rent(initialCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            foreach (var adapter in _poolAdapters.Values) {
                adapter.ReturnAll();
            }
        }
        
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;
            foreach (var adapter in _poolAdapters.Values) {
                adapter.Dispose();
            }
            
            _poolAdapters.Clear();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PoolAdapter<T> GetOrCreateAdapter<T>() {
            if (!_poolAdapters.TryGetValue(typeof(T), out var adapter)) {
                var newAdapter = new PoolAdapter<T>();
                _poolAdapters.Add(typeof(T), newAdapter);
                return newAdapter;
            }

            return Unsafe.As<PoolAdapter<T>>(adapter);
        }
    }
}