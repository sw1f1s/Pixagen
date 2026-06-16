using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;
namespace Pixagen.Ecs.Runtime {
    public sealed class Filter : IDisposable {
        private FilterMap _map;
        private IWorld _world;
        private SparseArray<Entity> _entities;
        private SparseArrayInt _dirtyEntities;
        
        private readonly int _mainComponent;
        private BitMask _includes;
        private BitMask _excludes;
        
        private bool _isDirty;
        private bool _isDisposed;
        
        internal SparseArray<Entity> Entities => _entities;
        public BitMask Includes => _includes;
        public BitMask Excludes => _excludes;
        
        internal IWorld World => _world;
        
        internal Filter(FilterMask mask, FilterMap map, IWorld world, in Options options) {
            _map = map;
            _world = world;
            _entities = new SparseArray<Entity>(options.EntityCapacity);
            _dirtyEntities = new SparseArrayInt(options.EntityCapacity);
            _mainComponent = mask.MainComponent;
            _includes = mask.GetIncludes();
            _excludes = mask.GetExcludes();
            _isDirty = true;
        }
        
        public Enumerator GetEnumerator() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
            
            UpdateIfDirty();
            return new Enumerator(this);
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public uint GetCount() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
            
            UpdateIfDirty();
            return _entities.Count;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void FillEntities(ref List<Entity> entities) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
            
            UpdateIfDirty();
            entities.Clear();
            foreach (var entity in this) {
                entities.Add(entity);
            }
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Entity GetFirstOrDefault() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().Name);
            }

            UpdateIfDirty();
            foreach (var entity in this) {
                return entity;
            }

            return Entity.Empty;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void SetDirty(int entityId) {
            if (entityId < 0) {
                return;
            }

            if (!_dirtyEntities.Has(in entityId)) {
                _dirtyEntities.Add(in entityId);
            }

            _isDirty = true;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void UpdateIfDirty() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
            
            _map.UpdateFiltersDirty();
            if (!_isDirty) {
                return;
            }

            if (_dirtyEntities.Count == 0) {
                RebuildAll();
            } else {
                UpdateDirtyEntities();
            }

            _dirtyEntities.Clear();
            _isDirty = false;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private void RebuildAll() {
            _entities.Clear();
            
            if (!_world.HasComponentStorage(_mainComponent)) {
                return;
            }
            
            var mainStorage = _world.GetComponentStorage(_mainComponent);
            foreach (var entityId in mainStorage.Entities) {
                if (!_world.Entities.Has(entityId)) {
                    continue;
                }

                if (Matches(entityId)) {
                    _entities.Add(entityId, _world.Entities.Get(entityId).GetEntity());
                }
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private void UpdateDirtyEntities() {
            foreach (var entityId in _dirtyEntities) {
                if (!_world.Entities.Has(entityId)) {
                    RemoveCached(entityId);

                    continue;
                }

                if (Matches(entityId)) {
                    _entities.Replace(entityId, _world.Entities.Get(entityId).GetEntity());
                }
                else {
                    RemoveCached(entityId);
                }
            }
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private bool Matches(int entityId) {
            foreach (var componentId in _includes) {
                if (!StorageHasEntity(componentId, entityId)) {
                    return false;
                }
            }

            foreach (var componentId in _excludes) {
                if (StorageHasEntity(componentId, entityId)) {
                    return false;
                }
            }

            return true;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private bool StorageHasEntity(int componentId, int entityId) {
            if (!_world.HasComponentStorage(componentId)) {
                return false;
            }
            
            return _world.GetComponentStorage(componentId).Entities.Has(in entityId);
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private void RemoveCached(int entityId) {
            if (_entities.Has(entityId)) {
                _entities.Remove(entityId);
            }
        }

        public void Dispose() {
            if (_isDisposed) {
                return;
            }
            
            _isDisposed = true;
            _world = null;
            _map = null;
            _includes.Dispose();
            _excludes.Dispose();
            _entities.Dispose();
            _dirtyEntities.Dispose();
        }
        
        public struct Enumerator : IDisposable {
            private SparseArray<Entity>.Enumerator<Entity> _cache;

            internal Enumerator(Filter filter) {
                _cache = filter._entities.GetEnumerator();
            }

            public Entity Current {
                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                get => _cache.Current;
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public bool MoveNext () {
                return _cache.MoveNext();
            }

            public void Dispose() {
                _cache.Dispose();
                _cache = default;
            }
        }
    }
}
