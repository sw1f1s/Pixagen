using System;
using System.Runtime.CompilerServices;

namespace Pixagen.Ecs.Runtime
{
    public readonly struct Entity : IEquatable<Entity>
    {
        private const int IdBits = 32;
        private const int GenBits = 24;
        private const int WorldIdBits = 8;
        private const int GenShift = IdBits;
        private const int WorldIdShift = IdBits + GenBits;
        private const ulong IdMask = (1UL << IdBits) - 1UL;
        private const ulong GenMask = (1UL << GenBits) - 1UL;
        private const ulong WorldIdMask = (1UL << WorldIdBits) - 1UL;
        private const int MaxGen = (int)GenMask - 1;

        public static readonly Entity Empty = default;
        private readonly long _value;

        private ulong RawValue => unchecked((ulong)_value);

        public int Id => (int)(RawValue & IdMask) - 1;
        public int Gen => (int)((RawValue >> GenShift) & GenMask) - 1;
        public byte WorldId => (byte)((RawValue >> WorldIdShift) & WorldIdMask);

        internal Entity(int id, int gen, byte worldId)
        {
            _value = Pack(id, gen, worldId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Entity IncreaseGen() =>
            new Entity(Id, checked(Gen + 1), WorldId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Entity ResetGen() =>
            new Entity(Id, 0, WorldId);

        public override bool Equals(object? obj)
        {
            return obj is Entity other && Equals(other);
        }

        public bool Equals(Entity other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"Entity[{Id}|{Gen}|{WorldId}]";
        }

        private static long Pack(int id, int gen, byte worldId)
        {
            if (id == -1 && gen == -1 && worldId == 0)
            {
                return 0;
            }

            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Entity id must be non-negative.");
            }

            if (gen < 0 || gen > MaxGen)
            {
                throw new ArgumentOutOfRangeException(nameof(gen), gen, $"Entity generation must be between 0 and {MaxGen}.");
            }

            ulong encodedId = (uint)id + 1UL;
            ulong encodedGen = (uint)gen + 1UL;
            ulong packed =
                encodedId |
                (encodedGen << GenShift) |
                ((ulong)worldId << WorldIdShift);

            return unchecked((long)packed);
        }
    }
}
