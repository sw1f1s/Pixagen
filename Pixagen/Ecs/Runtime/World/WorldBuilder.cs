using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;

namespace Pixagen.Ecs.Runtime
{
    public static class WorldBuilder
    {
        private const uint WORLD_CAPACITY = 2;
#if DEBUG
        public static event Action<IWorld> OnWorldCreated;
        public static event Action<IWorld> OnWorldDestroyed;
        public static ref SparseArray<IWorld> Worlds => ref _worlds;
#endif

        private static SparseArray<IWorld> _worlds = new SparseArray<IWorld>(WORLD_CAPACITY);
        private static byte[] _freeIndexes = new byte[2] { 2, 1 };
        private static byte _freeIndexesCount = 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IWorld Build()
        {
            var world = CreateWorld();
            _worlds.Add(world.Id, world);
#if DEBUG
            OnWorldCreated?.Invoke(world);
#endif
            return world;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AllDestroy()
        {
            var worlds = new List<IWorld>((int)_worlds.Count);
            foreach (var world in _worlds)
            {
                worlds.Add(world);
            }

            foreach (var world in worlds)
            {
                world.Dispose();
            }
            _worlds.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IWorld GetWorld(int worldId)
        {
            return _worlds.Get(worldId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AliveWorld(int worldId)
        {
            return _worlds.Has(worldId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(IWorld world)
        {
            if (!_worlds.Has(world.Id))
            {
                return;
            }

#if DEBUG
            OnWorldDestroyed?.Invoke(world);
#endif
            _worlds.Remove(world.Id);
            if (_freeIndexesCount >= _freeIndexes.Length)
            {
                Array.Resize(ref _freeIndexes, _freeIndexes.Length * 2);
            }

            _freeIndexes[_freeIndexesCount++] = world.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IWorld CreateWorld()
        {
            return CreateWorld(Options.Default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IWorld CreateWorld(Options options)
        {
            TryResize();
            byte index = _freeIndexes[--_freeIndexesCount];
            return new World(index, options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryResize()
        {
            if (_freeIndexesCount > 0)
            {
                return;
            }

            byte oldSize = (byte)_freeIndexes.Length;
            byte newSize = (byte)(oldSize * 2);
            Array.Resize(ref _freeIndexes, newSize);
            for (byte id = newSize; id > oldSize; id--)
            {
                _freeIndexes[_freeIndexesCount++] = id;
            }
        }
    }
}
