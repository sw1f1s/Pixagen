using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pixagen.Ecs.Collections;
namespace Pixagen.Ecs.Runtime
{
    public sealed class Filter : IDisposable
    {
        public const int DefaultChunkSize = 512;
        public const int DefaultParallelEntityThreshold = 4096;

        private FilterMap _map;
        private IWorld _world;
        private readonly object _cacheUpdateLock = new();
        private Entity[] _denseEntities;
        private int[] _sparseEntityIndexes;
        private int[] _chunkOffsets;
        private int _denseEntitiesCount;
        private SparseArrayInt _dirtyEntities;

        private readonly int _mainComponent;
        private BitMask _includes;
        private BitMask _excludes;

        private bool _isDirty;
        private bool _isDisposed;
        private int _version;

        public BitMask Includes => _includes;
        public BitMask Excludes => _excludes;

        internal IWorld World => _world;

        internal Filter(FilterMask mask, FilterMap map, IWorld world, in Options options)
        {
            _map = map;
            _world = world;
            _denseEntities = new Entity[options.EntityCapacity];
            _sparseEntityIndexes = new int[options.EntityCapacity];
            _chunkOffsets = new int[1];
            _dirtyEntities = new SparseArrayInt(options.EntityCapacity);
            _mainComponent = mask.MainComponent;
            _includes = mask.GetIncludes();
            _excludes = mask.GetExcludes();
            _isDirty = true;
        }

        public Enumerator GetEnumerator()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            UpdateIfDirty();
            return new Enumerator(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetCount()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            UpdateIfDirty();
            return (uint)_denseEntitiesCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillEntities(ref List<Entity> entities)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            UpdateIfDirty();
            entities.Clear();
            if (entities.Capacity < _denseEntitiesCount)
            {
                entities.Capacity = _denseEntitiesCount;
            }

            for (int i = 0; i < _denseEntitiesCount; i++)
            {
                entities.Add(_denseEntities[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetFirstOrDefault()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            UpdateIfDirty();
            if (_denseEntitiesCount > 0)
            {
                return _denseEntities[0];
            }

            return Entity.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<Entity> AsSpan()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            UpdateIfDirty();
            return _denseEntities.AsSpan(0, _denseEntitiesCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FilterChunks GetChunks(int chunkSize = DefaultChunkSize)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize), chunkSize, "Chunk size must be positive.");
            }

            UpdateIfDirty();
            return new FilterChunks(this, _denseEntities, _denseEntitiesCount, chunkSize, _version, _map.Version);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachChunk<TJob>(TJob job, int chunkSize = DefaultChunkSize)
            where TJob : struct, IFilterChunkProcessor
        {
            FilterChunkRunner.Run(GetChunks(chunkSize), job, allowParallel: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachChunkSequential<TJob>(TJob job, int chunkSize = DefaultChunkSize)
            where TJob : struct, IFilterChunkProcessor
        {
            FilterChunkRunner.Run(GetChunks(chunkSize), job, allowParallel: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetDirty(int entityId)
        {
            if (entityId < 0)
            {
                return;
            }

            if (!_dirtyEntities.Has(in entityId))
            {
                _dirtyEntities.Add(in entityId);
            }

            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateIfDirty()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            _map.UpdateFiltersDirty();
            if (!_isDirty)
            {
                return;
            }

            if (_dirtyEntities.Count == 0)
            {
                RebuildAll();
            }
            else
            {
                UpdateDirtyEntities();
            }

            _dirtyEntities.Clear();
            _isDirty = false;
            _version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RebuildAll()
        {
            ClearCached();

            if (!_world.HasComponentStorage(_mainComponent))
            {
                return;
            }

            var mainStorage = _world.GetComponentStorage(_mainComponent);
            var mainEntities = mainStorage.Entities;
            int candidateCount = (int)mainEntities.Count;
            if (candidateCount == 0)
            {
                return;
            }

            int[] candidateIds = mainEntities.DenseValues;
            if (ShouldRunParallel(candidateCount))
            {
                RebuildAllParallel(candidateIds, candidateCount);
                return;
            }

            RebuildAllSequential(candidateIds, candidateCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateDirtyEntities()
        {
            int dirtyCount = (int)_dirtyEntities.Count;
            if (dirtyCount == 0)
            {
                return;
            }

            int[] dirtyEntityIds = _dirtyEntities.DenseValues;
            if (ShouldRunParallel(dirtyCount))
            {
                UpdateDirtyEntitiesParallel(dirtyEntityIds, dirtyCount);
                return;
            }

            UpdateDirtyEntitiesSequential(dirtyEntityIds, dirtyCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RebuildAllSequential(int[] candidateIds, int candidateCount)
        {
            for (int i = 0; i < candidateCount; i++)
            {
                int entityId = candidateIds[i];
                if (_world.Entities.Has(entityId) && Matches(entityId))
                {
                    AddOrReplaceCached(entityId, _world.Entities.Get(entityId).GetEntity());
                }
            }
        }

        private void RebuildAllParallel(int[] candidateIds, int candidateCount)
        {
            int chunkCount = GetChunkCount(candidateCount, DefaultChunkSize);
            EnsureChunkCapacity(chunkCount);

            FilterChunkRunner.Run(
                chunkCount,
                new RebuildCountJob(this, candidateIds, candidateCount, _chunkOffsets));

            int total = 0;
            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int count = _chunkOffsets[chunkIndex];
                _chunkOffsets[chunkIndex] = total;
                total += count;
            }

            EnsureDenseCapacity(total);
            EnsureSparseCapacityForEntityIds(candidateIds, candidateCount);

            FilterChunkRunner.Run(
                chunkCount,
                new RebuildWriteJob(this, candidateIds, candidateCount, _chunkOffsets));

            _denseEntitiesCount = total;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateDirtyEntitiesSequential(int[] dirtyEntityIds, int dirtyCount)
        {
            for (int i = 0; i < dirtyCount; i++)
            {
                ApplyDirtyEntity(dirtyEntityIds[i], MatchesDirtyEntity(dirtyEntityIds[i]));
            }
        }

        private void UpdateDirtyEntitiesParallel(int[] dirtyEntityIds, int dirtyCount)
        {
            int chunkCount = GetChunkCount(dirtyCount, DefaultChunkSize);
            FilterChunkRunner.Run(chunkCount, new DirtyUpdateJob(this, dirtyEntityIds, dirtyCount));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte MatchesDirtyEntity(int entityId)
        {
            return (byte)(_world.Entities.Has(entityId) && Matches(entityId) ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyDirtyEntity(int entityId, byte matches)
        {
            if (matches == 0)
            {
                RemoveCached(entityId);
                return;
            }

            AddOrReplaceCached(entityId, _world.Entities.Get(entityId).GetEntity());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Matches(int entityId)
        {
            foreach (var componentId in _includes)
            {
                if (!StorageHasEntity(componentId, entityId))
                {
                    return false;
                }
            }

            foreach (var componentId in _excludes)
            {
                if (StorageHasEntity(componentId, entityId))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool StorageHasEntity(int componentId, int entityId)
        {
            if (!_world.HasComponentStorage(componentId))
            {
                return false;
            }

            return _world.GetComponentStorage(componentId).Entities.Has(in entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddOrReplaceCached(int entityId, Entity entity)
        {
            EnsureCapacity(entityId);
            int denseIndex = _sparseEntityIndexes[entityId] - 1;
            if (denseIndex >= 0)
            {
                _denseEntities[denseIndex] = entity;
                return;
            }

            _denseEntities[_denseEntitiesCount] = entity;
            _sparseEntityIndexes[entityId] = ++_denseEntitiesCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddCachedAt(int denseIndex, int entityId, Entity entity)
        {
            _denseEntities[denseIndex] = entity;
            _sparseEntityIndexes[entityId] = denseIndex + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveCached(int entityId)
        {
            if (entityId < 0 || entityId >= _sparseEntityIndexes.Length)
            {
                return;
            }

            int denseIndex = _sparseEntityIndexes[entityId] - 1;
            if (denseIndex < 0)
            {
                return;
            }

            _sparseEntityIndexes[entityId] = 0;
            int lastIndex = --_denseEntitiesCount;
            if (lastIndex > denseIndex)
            {
                Entity moved = _denseEntities[lastIndex];
                _denseEntities[denseIndex] = moved;
                _sparseEntityIndexes[moved.Id] = denseIndex + 1;
            }

            _denseEntities[lastIndex] = Entity.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearCached()
        {
            for (int i = 0; i < _denseEntitiesCount; i++)
            {
                int entityId = _denseEntities[i].Id;
                if (entityId >= 0 && entityId < _sparseEntityIndexes.Length)
                {
                    _sparseEntityIndexes[entityId] = 0;
                }

                _denseEntities[i] = Entity.Empty;
            }

            _denseEntitiesCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int entityId)
        {
            while (_denseEntitiesCount >= _denseEntities.Length || entityId >= _sparseEntityIndexes.Length)
            {
                int newSize = Math.Max(1, _denseEntities.Length * 2);
                Array.Resize(ref _denseEntities, newSize);
                Array.Resize(ref _sparseEntityIndexes, newSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureDenseCapacity(int count)
        {
            while (count > _denseEntities.Length)
            {
                int newSize = Math.Max(1, _denseEntities.Length * 2);
                Array.Resize(ref _denseEntities, newSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSparseCapacity(int entityId)
        {
            while (entityId >= _sparseEntityIndexes.Length)
            {
                int newSize = Math.Max(1, _sparseEntityIndexes.Length * 2);
                Array.Resize(ref _sparseEntityIndexes, newSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSparseCapacityForEntityIds(int[] entityIds, int count)
        {
            int maxEntityId = -1;
            for (int i = 0; i < count; i++)
            {
                if (entityIds[i] > maxEntityId)
                {
                    maxEntityId = entityIds[i];
                }
            }

            EnsureSparseCapacity(maxEntityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureChunkCapacity(int chunkCount)
        {
            if (_chunkOffsets.Length < chunkCount)
            {
                Array.Resize(ref _chunkOffsets, chunkCount);
            }
        }

        internal void ValidateChunks(int filterVersion, int mapVersion)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (_version != filterVersion || _map.Version != mapVersion)
            {
                throw new InvalidOperationException("Filter chunks are stale. Request chunks again after ECS structural changes.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldRunParallel(int entityCount)
        {
            return entityCount >= DefaultParallelEntityThreshold && Environment.ProcessorCount > 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetChunkCount(int entityCount, int chunkSize)
        {
            return (entityCount + chunkSize - 1) / chunkSize;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _world = null;
            _map = null;
            _includes.Dispose();
            _excludes.Dispose();
            _denseEntities = null;
            _sparseEntityIndexes = null;
            _chunkOffsets = null;
            _denseEntitiesCount = 0;
            _dirtyEntities.Dispose();
        }

        private readonly struct RebuildCountJob : IFilterChunkJob
        {
            private readonly Filter _filter;
            private readonly int[] _candidateIds;
            private readonly int _candidateCount;
            private readonly int[] _chunkCounts;

            public RebuildCountJob(Filter filter, int[] candidateIds, int candidateCount, int[] chunkCounts)
            {
                _filter = filter;
                _candidateIds = candidateIds;
                _candidateCount = candidateCount;
                _chunkCounts = chunkCounts;
            }

            public void Execute(int chunkIndex)
            {
                int start = chunkIndex * DefaultChunkSize;
                int end = Math.Min(start + DefaultChunkSize, _candidateCount);
                int count = 0;
                for (int i = start; i < end; i++)
                {
                    int entityId = _candidateIds[i];
                    if (_filter._world.Entities.Has(entityId) && _filter.Matches(entityId))
                    {
                        count++;
                    }
                }

                _chunkCounts[chunkIndex] = count;
            }
        }

        private readonly struct RebuildWriteJob : IFilterChunkJob
        {
            private readonly Filter _filter;
            private readonly int[] _candidateIds;
            private readonly int _candidateCount;
            private readonly int[] _chunkStarts;

            public RebuildWriteJob(Filter filter, int[] candidateIds, int candidateCount, int[] chunkStarts)
            {
                _filter = filter;
                _candidateIds = candidateIds;
                _candidateCount = candidateCount;
                _chunkStarts = chunkStarts;
            }

            public void Execute(int chunkIndex)
            {
                int start = chunkIndex * DefaultChunkSize;
                int end = Math.Min(start + DefaultChunkSize, _candidateCount);
                int writeIndex = _chunkStarts[chunkIndex];
                for (int i = start; i < end; i++)
                {
                    int entityId = _candidateIds[i];
                    if (!_filter._world.Entities.Has(entityId) || !_filter.Matches(entityId))
                    {
                        continue;
                    }

                    _filter.AddCachedAt(entityId: entityId, denseIndex: writeIndex, entity: _filter._world.Entities.Get(entityId).GetEntity());
                    writeIndex++;
                }
            }
        }

        private readonly struct DirtyUpdateJob : IFilterChunkJob
        {
            private readonly Filter _filter;
            private readonly int[] _dirtyEntityIds;
            private readonly int _dirtyCount;

            public DirtyUpdateJob(Filter filter, int[] dirtyEntityIds, int dirtyCount)
            {
                _filter = filter;
                _dirtyEntityIds = dirtyEntityIds;
                _dirtyCount = dirtyCount;
            }

            public void Execute(int chunkIndex)
            {
                int start = chunkIndex * DefaultChunkSize;
                int end = Math.Min(start + DefaultChunkSize, _dirtyCount);
                for (int i = start; i < end; i++)
                {
                    int entityId = _dirtyEntityIds[i];
                    byte matches = _filter.MatchesDirtyEntity(entityId);
                    lock (_filter._cacheUpdateLock)
                    {
                        _filter.ApplyDirtyEntity(entityId, matches);
                    }
                }
            }
        }

        public struct Enumerator : IDisposable
        {
            private Entity[] _entities;
            private int _count;
            private int _idx;

            internal Enumerator(Filter filter)
            {
                _entities = filter._denseEntities;
                _count = filter._denseEntitiesCount;
                _idx = -1;
            }

            public Entity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _entities[_idx];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_idx < _count;
            }

            public void Dispose()
            {
                _entities = null;
                _count = 0;
                _idx = 0;
            }
        }
    }
}
