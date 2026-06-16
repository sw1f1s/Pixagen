using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Pixagen.Ecs.DI
{
    public struct ComponentInject<T> : IDataInject where T : struct, IComponent
    {
        private World _world;
        private ComponentStorage<T> _storage;

        public readonly int Version => _storage.Version;

        public ComponentInject(IWorld world)
        {
            _world = world as World
                ?? throw new InvalidOperationException($"{nameof(ComponentInject<T>)} requires {nameof(World)}.");
            _storage = _world.GetComponentStorage<T>();
        }

        void IDataInject.Fill(ISystems systems)
        {
            this = new ComponentInject<T>(systems.World);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Has(Entity entity)
        {
#if DEBUG
            if (entity != Entity.Empty && !_world.EntityIsAlive(entity))
            {
                throw new Exception($"{entity} is dead.");
            }
#endif
            return _storage.HasFast(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public readonly ref T Get(Entity entity)
        {
#if DEBUG
            if (!_world.EntityIsAlive(entity))
            {
                throw new Exception($"{entity} is dead.");
            }
#endif
            return ref _storage.GetFast(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public readonly ref T Set(Entity entity)
        {
#if DEBUG
            if (!_world.EntityIsAlive(entity))
            {
                throw new Exception($"{entity} is dead.");
            }
#endif
            return ref _world.SetComponent(entity, _storage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public readonly ref T GetOrSet(Entity entity)
        {
            if (_storage.HasFast(entity))
            {
                return ref _storage.GetFast(entity);
            }

            return ref Set(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Entity Add(Entity entity, in T component)
        {
#if DEBUG
            if (!_world.EntityIsAlive(entity))
            {
                throw new Exception($"{entity} is dead.");
            }
#endif
            _world.AddComponent(in entity, in component, _storage);
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Entity Replace(Entity entity, in T component)
        {
#if DEBUG
            if (!_world.EntityIsAlive(entity))
            {
                throw new Exception($"{entity} is dead.");
            }
#endif
            _world.ReplaceComponent(in entity, in component, _storage);
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Remove(Entity entity)
        {
#if DEBUG
            if (!_world.EntityIsAlive(entity))
            {
                throw new Exception($"{entity} is dead.");
            }
#endif
            _world.RemoveComponent(entity, _storage);
        }
    }
}
