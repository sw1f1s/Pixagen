using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;
namespace Pixagen.Ecs.Runtime
{
    internal sealed class ComponentStorage<T> : IComponentStorage where T : struct, IComponent
    {
        private readonly T _defaultInstance = default;
        private PoolFactory _poolFactory;
        private IComponentStorage.AutoResetHandler<T> _autoResetHandler;
        private IComponentStorage.AutoCopyHandler<T> _autoCopyHandler;
        private IComponentStorage.AutoDestroyHandler<T> _autoDestroyHandler;
        private IComponentStorage.AutoPoolResetHandler<T> _autoPoolResetHandler;
        private IComponentStorage.AutoPoolDestroyHandler<T> _autoPoolDestroyHandler;
        private SparseArrayInt _entities;
        private SparseArray<T> _components;
        private bool _isDisposed;
        private int _version;

        public bool IsOneTickComponent { get; private set; }
        public Type ComponentType => typeof(T);
        public int Id => ComponentStorageIndex<T>.StaticId;
        public int Count => (int)_components.Count;
        public int Version => _version;
        public ref SparseArrayInt Entities => ref _entities;

        internal ComponentStorage(PoolFactory poolFactory, in Options options)
        {
            _poolFactory = poolFactory;
            _components = new SparseArray<T>(options.EntityCapacity);
            _entities = new SparseArrayInt(options.EntityCapacity);
            if (IComponentStorage.TryGetInterface(ref _defaultInstance, out IAutoCopyComponent<T> autoCopy))
            {
                _autoCopyHandler = autoCopy.Copy;
            }

            if (IComponentStorage.TryGetInterface(ref _defaultInstance, out IAutoResetComponent<T> autoReset))
            {
                _autoResetHandler = autoReset.Reset;
            }

            if (IComponentStorage.TryGetInterface(ref _defaultInstance, out IAutoDestroyComponent<T> autoDestroy))
            {
                _autoDestroyHandler = autoDestroy.Destroy;
            }

            if (IComponentStorage.TryGetInterface(ref _defaultInstance, out IAutoPoolComponent<T> autoPool))
            {
                _autoPoolResetHandler = autoPool.Reset;
                _autoPoolDestroyHandler = autoPool.Destroy;
            }

            IsOneTickComponent = _defaultInstance is IOneTickComponent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent(in Entity entity, in T component)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentStorage<T>));
            }

            if (HasComponentInternal(entity))
            {
                throw new Exception($"{entity} already contains {typeof(T).Name}");
            }
            AddComponentInternal(entity, in component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReplaceComponent(in Entity entity, in T component)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentStorage<T>));
            }

            bool hadComponent = HasComponentInternal(entity);
            ReplaceComponentInternal(entity, in component);
            return hadComponent;
        }

        public IComponent GetGeneralizedComponent(Entity entity)
        {
            return GetComponent(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent(Entity entity)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentStorage<T>));
            }

            if (!HasComponentInternal(entity))
            {
                throw new Exception($"{entity} not contains {typeof(T).Name}");
            }

            return ref GetComponentInternal(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T SetComponent(Entity entity)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentStorage<T>));
            }

            if (HasComponentInternal(entity))
            {
                throw new Exception($"{entity} already contains {typeof(T).Name}");
            }

            var newComponent = new T();
            _autoResetHandler?.Invoke(ref newComponent);
            _autoPoolResetHandler?.Invoke(ref newComponent, _poolFactory);
            AddComponentInternal(entity, newComponent);
            return ref GetComponentInternal(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(Entity entity)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentStorage<T>));
            }

            return HasComponentInternal(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasFast(Entity entity)
        {
            return HasComponentInternal(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref T GetFast(Entity entity)
        {
#if DEBUG
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentStorage<T>));
            }

            if (!HasComponentInternal(entity))
            {
                throw new Exception($"{entity} not contains {typeof(T).Name}");
            }
#endif

            return ref GetComponentInternal(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent(in Entity entity)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentStorage<T>));
            }

            if (HasComponentInternal(entity))
            {
                ref var component = ref GetComponentInternal(entity);
                _autoDestroyHandler?.Invoke(ref component);
                _autoPoolDestroyHandler?.Invoke(ref component, _poolFactory);
                int entityId = entity.Id;
                _components.Remove(entityId);
                _entities.Remove(in entityId);
                _version++;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyComponent(in Entity fromEntity, in Entity toEntity)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ComponentStorage<T>));
            }

            if (!HasComponentInternal(fromEntity) || HasComponentInternal(toEntity))
            {
                return;
            }

            T srcComponent = GetComponent(fromEntity);
            var newComponent = srcComponent;

            _autoCopyHandler?.Invoke(ref srcComponent, ref newComponent);
            AddComponentInternal(toEntity, newComponent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddComponentInternal(in Entity entity, in T component)
        {
            int entityId = entity.Id;
            _components.Add(entityId, in component);
            _entities.Add(in entityId);
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReplaceComponentInternal(in Entity entity, in T component)
        {
            int entityId = entity.Id;
            _components.Replace(entityId, in component);
            _entities.Replace(in entityId);
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasComponentInternal(Entity entity)
        {
            return _components.Has(entity.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetComponentInternal(Entity entity)
        {
            return ref _components.Get(entity.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IComponentStorage.Clear()
        {
            _components.Clear();
            _entities.Clear();
            _version++;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _autoResetHandler = null;
            _autoCopyHandler = null;
            _autoDestroyHandler = null;
            _autoPoolResetHandler = null;
            _autoPoolDestroyHandler = null;
            _components.Dispose();
            _components = default;
            _entities.Dispose();
            _entities = default;
            _poolFactory = null;
        }
    }
}
