using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;

namespace Pixagen.Ecs.Runtime
{
    public sealed class World : IWorld
    {
        public byte Id { get; private set; }

        private readonly EntityRegistry _entityRegistry;
        private readonly FilterMap _filterMap;
        private readonly ComponentsStorage _componentsStorage;
        private readonly PoolFactory _poolFactory;
        private Options _options;
        private bool _isDisposed;

        bool IWorld.IsAlive => !_isDisposed;

        IComponentsStorage IWorld.ComponentsStorage => _componentsStorage;
        Options IWorld.Options => _options;

        ref SparseArray<EntityData> IWorld.Entities => ref _entityRegistry.Entities;
        internal World(byte id, in Options options)
        {
            Id = id;
            _options = options;
            _entityRegistry = new EntityRegistry(id, options.EntityCapacity);
            _filterMap = new FilterMap(this, in options);
            _poolFactory = new PoolFactory();
            _componentsStorage = new ComponentsStorage(_poolFactory, in options);
        }

        #region Entities

#if DEBUG
        public event Action<IWorld, Entity> OnCreateEntity;
        public event Action<IWorld, Entity> OnCopyEntity;
        public event Action<IWorld, Entity> OnDestroyEntity;
        public event Action<IWorld, Entity, Type> OnAddComponent;
        public event Action<IWorld, Entity, Type> OnRemoveComponent;
        IEnumerable<Entity> IWorld.AllEntities()
        {
            foreach (var entity in _entityRegistry.Entities)
            {
                yield return entity.GetEntity();
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity<T>() where T : struct, IComponent
        {
            var entity = CreateEntityInternal();
#if DEBUG
            OnCreateEntity?.Invoke(this, entity);
#endif
            SetComponent(entity, _componentsStorage.GetComponentStorage<T>());
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Entity IWorld.CreateEntity(int id, int gen)
        {
            if (_entityRegistry.Has(id))
            {
                var entityData = _entityRegistry.Get(id);
                entityData.IncreaseGen(gen);
                return entityData.GetEntity();
            }

            ref var newEntityData = ref _entityRegistry.GetFreeEntity(id, gen);
            var entity = newEntityData.GetEntity();
#if DEBUG
            OnCreateEntity?.Invoke(this, entity);
#endif
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Entity IWorld.TryGetEntity(int id)
        {
            if (_entityRegistry.Has(id))
            {
                return _entityRegistry.Get(id).GetEntity();
            }

            return Entity.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CopyEntity(in Entity entity)
        {
            var copyEntity = CreateEntityInternal();
            ref var copyEntityData = ref _entityRegistry.Get(copyEntity);
            foreach (var componentStorage in _componentsStorage.Storages)
            {
                if (!componentStorage.HasComponent(entity))
                {
                    continue;
                }

                componentStorage.CopyComponent(entity, copyEntity);
                copyEntityData.AddComponent();
                _filterMap.AddDirtyEntity(componentStorage.Id, copyEntity.Id, FilterDirtyOperation.Add);
            }
#if DEBUG
            OnCopyEntity?.Invoke(this, copyEntity);
#endif
            return copyEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IWorld.EntityIsAlive(in Entity entity)
        {
            return EntityIsAlive(in entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool EntityIsAlive(in Entity entity)
        {
            if (!_entityRegistry.Has(entity))
            {
                return false;
            }

            ref var entityData = ref _entityRegistry.Get(entity);
            return entityData.GetEntity().Gen == entity.Gen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IWorld.DestroyEntity(in Entity entity)
        {
#if DEBUG
            OnDestroyEntity?.Invoke(this, entity);
#endif

            var entityData = _entityRegistry.Get(entity);
            foreach (var componentStorage in _componentsStorage.Storages)
            {
                if (componentStorage.RemoveComponent(entity))
                {
                    _filterMap.AddDirtyEntity(componentStorage.Id, entity.Id, FilterDirtyOperation.Remove);
                }
            }
            _entityRegistry.Return(entityData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IWorld.HasComponent<T>(in Entity entity)
        {
            var storage = _componentsStorage.GetComponentStorage<T>();
            return storage.HasComponent(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ref T IWorld.GetComponent<T>(in Entity entity)
        {
            var storage = _componentsStorage.GetComponentStorage<T>();
            return ref storage.GetComponent(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IWorld.AddComponent<T>(in Entity entity, in T component)
        {
            var storage = _componentsStorage.GetComponentStorage<T>();
            AddComponent(in entity, in component, storage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddComponent<T>(in Entity entity, in T component, ComponentStorage<T> storage) where T : struct, IComponent
        {
            storage.AddComponent(entity, in component);
            _entityRegistry.Get(entity).AddComponent();
            _filterMap.AddDirtyEntity(storage.Id, entity.Id, FilterDirtyOperation.Add);
#if DEBUG
            OnAddComponent?.Invoke(this, entity, typeof(T));
#endif
        }

        void IWorld.ReplaceComponent<T>(in Entity entity, in T component)
        {
            var storage = _componentsStorage.GetComponentStorage<T>();
            ReplaceComponent(in entity, in component, storage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReplaceComponent<T>(in Entity entity, in T component, ComponentStorage<T> storage) where T : struct, IComponent
        {
            bool hadComponent = storage.ReplaceComponent(entity, in component);
            if (!hadComponent)
            {
                _entityRegistry.Get(entity).AddComponent();
                _filterMap.AddDirtyEntity(storage.Id, entity.Id, FilterDirtyOperation.Add);
            }
#if DEBUG
            OnAddComponent?.Invoke(this, entity, typeof(T));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ref T IWorld.SetComponent<T>(in Entity entity)
        {
            var storage = _componentsStorage.GetComponentStorage<T>();
            return ref SetComponent(entity, storage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref T SetComponent<T>(Entity entity, ComponentStorage<T> storage) where T : struct, IComponent
        {
            ref T component = ref storage.SetComponent(entity);
            _entityRegistry.Get(entity).AddComponent();
            _filterMap.AddDirtyEntity(storage.Id, entity.Id, FilterDirtyOperation.Add);
#if DEBUG
            OnAddComponent?.Invoke(this, entity, typeof(T));
#endif
            return ref component;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IWorld.RemoveComponent<T>(in Entity entity)
        {
            var storage = _componentsStorage.GetComponentStorage<T>();
            RemoveComponent(entity, storage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IWorld.RemoveComponent(in Entity entity, int componentIdx)
        {
            var storage = _componentsStorage.Get(componentIdx);
            RemoveComponentInternal(entity, storage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveComponent<T>(Entity entity, ComponentStorage<T> storage) where T : struct, IComponent
        {
            RemoveComponentInternal(entity, storage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveComponent(in Entity entity, IComponentStorage storage)
        {
            RemoveComponentInternal(entity, storage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity CreateEntityInternal()
        {
            ref var entityData = ref _entityRegistry.GetFreeEntity();
            return entityData.GetEntity();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveComponentInternal(in Entity entity, IComponentStorage storage)
        {
            if (storage.RemoveComponent(entity))
            {
                ref var entityData = ref _entityRegistry.Get(entity);
                entityData.RemoveComponent();
                _filterMap.AddDirtyEntity(storage.Id, entity.Id, FilterDirtyOperation.Remove);
                if (entityData.IsEmpty)
                {
                    _entityRegistry.Return(entityData);
                }
            }

#if DEBUG
            OnRemoveComponent?.Invoke(this, entity, storage.ComponentType);
#endif
        }
        #endregion

        #region Components
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IComponentStorage IWorld.GetComponentStorage(int componentId)
        {
            return _componentsStorage.Get(componentId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ComponentStorage<T> IWorld.GetComponentStorage<T>()
        {
            return GetComponentStorage<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentStorage<T> GetComponentStorage<T>() where T : struct, IComponent
        {
            return _componentsStorage.GetComponentStorage<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IWorld.HasComponentStorage(int componentId)
        {
            return _componentsStorage.Has(componentId);
        }
        #endregion

        #region Filters
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Filter GetFilter(FilterMask mask)
        {
            return _filterMap.GetFilter(mask);
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _filterMap.Clear();
            _componentsStorage.Clear();
            _entityRegistry.Clear();
            _poolFactory.Clear();
        }

        ~World() =>
            Dispose();

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            WorldBuilder.Destroy(this);
            _isDisposed = true;
            _filterMap.Dispose();
            _entityRegistry.Dispose();
            _componentsStorage.Dispose();
            _poolFactory.Dispose();
        }
    }
}
