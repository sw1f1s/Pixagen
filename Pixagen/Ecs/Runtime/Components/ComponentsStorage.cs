using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;
namespace Pixagen.Ecs.Runtime
{
    internal class ComponentsStorage : IComponentsStorage, IDisposable
    {
        private readonly List<int> _oneTickComponents = new List<int>();
        private PoolFactory _poolFactory;
        private SparseArray<IComponentStorage> _components;
        private Options _options;
        private bool _isDisposed;

        public IReadOnlyList<int> OneTickStorages => _oneTickComponents;
        public ref SparseArray<IComponentStorage> Storages => ref _components;

        public ComponentsStorage(PoolFactory poolFactory, in Options options)
        {
            _poolFactory = poolFactory;
            _components = new SparseArray<IComponentStorage>(options.ComponentCapacity);
            _options = options;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IComponentStorage Get(int componentId)
        {
            return _components.Get(componentId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Has(int componentId)
        {
            return _components.Has(componentId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentStorage<T> GetComponentStorage<T>() where T : struct, IComponent
        {
            int componentId = ComponentStorageIndex<T>.StaticId;
            if (!_components.Has(componentId))
            {
                var storage = new ComponentStorage<T>(_poolFactory, _options);
                _components.Add(componentId, storage);
                if (storage.IsOneTickComponent)
                {
                    _oneTickComponents.Add(componentId);
                }
            }

            return Unsafe.As<ComponentStorage<T>>(_components.Get(componentId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            foreach (var component in _components)
            {
                component.Clear();
            }
            _oneTickComponents.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _components.Dispose();
            _oneTickComponents.Clear();
            _poolFactory = null;
        }
    }
}
