using System;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;
namespace Pixagen.Ecs.Runtime
{
    internal class EntityRegistry : IDisposable
    {
        private readonly byte _worldId;
        private SparseArray<EntityData> _entities;
        private SparseArray<EntityData> _pool;
        private bool _isDisposed;
        public ref SparseArray<EntityData> Entities => ref _entities;

        public EntityRegistry(byte worldId, uint capacity)
        {
            _worldId = worldId;
            _entities = new SparseArray<EntityData>(capacity);
            _pool = new SparseArray<EntityData>(capacity);
            for (int i = _pool.Length - 1; i >= 0; i--)
            {
                var data = new EntityData(new Entity(i, 0, _worldId));
                _pool.Add(i, data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity entity)
        {
            return _entities.Has(entity.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int id)
        {
            return _entities.Has(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityData Get(Entity entity)
        {
            return ref _entities.Get(entity.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityData Get(int id)
        {
            return ref _entities.Get(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityData GetFreeEntity()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(EntityRegistry));
            }

            TryIncreasePool();
            var entity = _pool.GetLast();
            entity.IncreaseGen();
            _pool.Remove(entity.Id);
            _entities.Add(entity.Id, entity);
            return ref _entities.Get(entity.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityData GetFreeEntity(int id, int gen)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(EntityRegistry));
            }

            TryIncreasePool(id);

            var entity = _pool.Get(id);
            entity.IncreaseGen(gen);
            _pool.Remove(entity.Id);
            _entities.Add(entity.Id, entity);
            return ref _entities.Get(entity.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(EntityData entity)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(EntityRegistry));
            }
            entity.ClearComponents();
            _entities.Remove(entity.Id);
            _pool.Add(entity.Id, entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(EntityRegistry));
            }

            foreach (ref var entity in _entities)
            {
                _pool.Add(entity.Id, entity);
            }
            _entities.Clear();
            foreach (ref var entity in _pool)
            {
                entity.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryIncreasePool()
        {
            if (_pool.Count > 0)
            {
                return;
            }

            int length = _pool.Length;
            int newLength = length * 2 - 1;
            for (int i = newLength; i >= length; i--)
            {
                var data = new EntityData(new Entity(i, 0, _worldId));
                _pool.Add(i, data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryIncreasePool(int minCount)
        {
            if (_pool.Length > minCount)
            {
                return;
            }

            int length = _pool.Length;
            int newLength = length * 2;
            while (newLength <= minCount)
            {
                newLength *= 2;
            }

            for (int i = newLength - 1; i >= length; i--)
            {
                var data = new EntityData(new Entity(i, 0, _worldId));
                _pool.Add(i, data);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (ref var entity in _entities)
            {
                entity.Dispose();
            }

            foreach (ref var entity in _pool)
            {
                entity.Dispose();
            }
            _entities.Dispose();
            _pool.Dispose();
            _entities = default;
            _pool = default;
        }
    }
}
