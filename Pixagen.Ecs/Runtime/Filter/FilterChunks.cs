using System;
using System.Runtime.CompilerServices;

namespace Pixagen.Ecs.Runtime
{
    public readonly struct FilterChunk
    {
        private readonly Filter? _owner;
        private readonly Entity[]? _entities;
        private readonly int _start;
        private readonly int _filterVersion;
        private readonly int _mapVersion;

        internal FilterChunk(Filter owner, Entity[] entities, int start, int count, int filterVersion, int mapVersion)
        {
            _owner = owner;
            _entities = entities;
            _start = start;
            _filterVersion = filterVersion;
            _mapVersion = mapVersion;
            Count = count;
        }

        public int Count { get; }

        public FilterChunkEntities Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Validate();
                return new FilterChunkEntities(_owner, _entities, _start, Count, _filterVersion, _mapVersion);
            }
        }

        public ReadOnlySpan<Entity> AsSpan()
        {
            Validate();
            return _entities is null ? ReadOnlySpan<Entity>.Empty : _entities.AsSpan(_start, Count);
        }

        public Entity this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Validate();
                if ((uint)index >= (uint)Count || _entities is null)
                {
                    throw new IndexOutOfRangeException();
                }

                return _entities[_start + index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Validate()
        {
            _owner?.ValidateChunks(_filterVersion, _mapVersion);
        }
    }

    public readonly struct FilterChunkEntities
    {
        private readonly Filter? _owner;
        private readonly Entity[]? _entities;
        private readonly int _start;
        private readonly int _filterVersion;
        private readonly int _mapVersion;

        internal FilterChunkEntities(Filter? owner, Entity[]? entities, int start, int count, int filterVersion, int mapVersion)
        {
            _owner = owner;
            _entities = entities;
            _start = start;
            _filterVersion = filterVersion;
            _mapVersion = mapVersion;
            Count = count;
        }

        public int Count { get; }

        public Entity this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Validate();
                if ((uint)index >= (uint)Count || _entities is null)
                {
                    throw new IndexOutOfRangeException();
                }

                return _entities[_start + index];
            }
        }

        public ReadOnlySpan<Entity> AsSpan()
        {
            Validate();
            return _entities is null ? ReadOnlySpan<Entity>.Empty : _entities.AsSpan(_start, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            Validate();
            return new Enumerator(_entities, _start, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Validate()
        {
            _owner?.ValidateChunks(_filterVersion, _mapVersion);
        }

        public struct Enumerator
        {
            private readonly Entity[]? _entities;
            private readonly int _end;
            private int _index;

            internal Enumerator(Entity[]? entities, int start, int count)
            {
                _entities = entities;
                _index = start - 1;
                _end = start + count;
            }

            public readonly Entity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _entities![_index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return _entities is not null && ++_index < _end;
            }
        }
    }

    public readonly struct FilterChunks
    {
        private readonly Filter? _owner;
        private readonly Entity[]? _entities;
        private readonly int _chunkSize;
        private readonly int _filterVersion;
        private readonly int _mapVersion;
        private readonly int _entityCount;
        private readonly int _count;

        internal FilterChunks(Filter owner, Entity[] entities, int entityCount, int chunkSize, int filterVersion, int mapVersion)
        {
            _owner = owner;
            _entities = entities;
            _chunkSize = chunkSize;
            _filterVersion = filterVersion;
            _mapVersion = mapVersion;
            _entityCount = entityCount;
            _count = entityCount == 0 ? 0 : (entityCount + chunkSize - 1) / chunkSize;
        }

        public int EntityCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Validate();
                return _entityCount;
            }
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Validate();
                return _count;
            }
        }

        public FilterChunk this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Validate();
                if ((uint)index >= (uint)_count || _entities is null || _owner is null)
                {
                    throw new IndexOutOfRangeException();
                }

                int start = index * _chunkSize;
                int count = Math.Min(_chunkSize, _entityCount - start);
                return new FilterChunk(_owner, _entities, start, count, _filterVersion, _mapVersion);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IDisposable
        {
            private FilterChunks _chunks;
            private int _idx;

            internal Enumerator(FilterChunks chunks)
            {
                _chunks = chunks;
                _idx = -1;
            }

            public readonly FilterChunk Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _chunks[_idx];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_idx < _chunks.Count;
            }

            public void Dispose()
            {
                _chunks = default;
                _idx = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Validate()
        {
            _owner?.ValidateChunks(_filterVersion, _mapVersion);
        }
    }
}
