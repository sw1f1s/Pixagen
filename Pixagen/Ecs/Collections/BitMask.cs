using System;
using System.Runtime.CompilerServices;
namespace Pixagen.Ecs.Collections {
    public struct BitMask : IDisposable {
        private const int BitsPerElement = 32;
        private uint[] _bits;
        private uint _count;
        private bool _isDisposed;
        
        public uint Count => _count;
        public int Hash => GetHashId();
        
        public BitMask(int capacity) {
            int arraySize = (capacity + BitsPerElement - 1) / BitsPerElement;
            _bits = new uint[arraySize];
            _count = 0;
            _isDisposed = false;
        }

        private BitMask(in BitMask copy) {
            if (copy._isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            _bits = new uint[copy._bits.Length];
            _count = copy._count;
            Array.Copy(copy._bits, _bits, copy._bits.Length);
            _isDisposed = false;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public BitMask Clone() {
            return new BitMask(this);
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Set(int id) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            var (arrayIndex, bitIndex) = GetIndices(id);
            TryResize(arrayIndex);
            _bits[arrayIndex] |= 1u << bitIndex;
            _count++;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Unset(int id) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
                
            }
            var (arrayIndex, bitIndex) = GetIndices(id);
            if (arrayIndex < _bits.Length) {
                _bits[arrayIndex] &= ~(1u << bitIndex);
            }
            _count--;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool Has(int id) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            var (arrayIndex, bitIndex) = GetIndices(id);
            return arrayIndex < _bits.Length && (_bits[arrayIndex] & (1u << bitIndex)) != 0;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            Array.Clear(_bits, 0, _bits.Length);
            _count = 0;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool HasAllCollision(in BitMask other) {
            if (_isDisposed || other._isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            for (int i = 0; i < other._bits.Length; i++) {
                uint bit = i >= _bits.Length ? 0 : _bits[i];
                if ((bit & other._bits[i]) != other._bits[i]) {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool HasAnyCollision(in BitMask other) {
            if (_isDisposed || other._isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            int minLength = Math.Min(_bits.Length, other._bits.Length);
            for (int i = 0; i < minLength; i++) {
                if ((_bits[i] & other._bits[i]) != 0) {
                    return true;
                }
            }
            return false;
        }
        
        public Enumerator GetEnumerator() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            return new Enumerator(this);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private int GetHashId() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            unchecked {
                const int prime = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < _bits.Length; i++) {
                    hash = (hash * prime) ^ ((i * 397) ^ (int)_bits[i]);
                }

                return hash;
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private void TryResize(int minCapacity) {
            while (_bits.Length <= minCapacity) {
                Array.Resize(ref _bits, _bits.Length * 2);
            }
        }
        
        public void Dispose() {
            if (_isDisposed) {
                return;
            }
            
            _isDisposed = true;
            _bits = null;
            _count = 0;
        }

        public static BitMask operator &(in BitMask a, in BitMask b) {
            if (a._isDisposed || b._isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            int resultLength = Math.Min(a._bits.Length, b._bits.Length);
            BitMask result = new BitMask(resultLength * BitsPerElement);
        
            for (int i = 0; i < resultLength; i++) {
                result._bits[i] = a._bits[i] & b._bits[i];
            }
        
            return result;
        }
    
        public static BitMask operator |(in BitMask a, in BitMask b) {
            if (a._isDisposed || b._isDisposed) {
                throw new ObjectDisposedException(nameof(BitMask));
            }
            
            int resultLength = Math.Max(a._bits.Length, b._bits.Length);
            BitMask result = new BitMask(resultLength * BitsPerElement);
            int minLength = Math.Min(a._bits.Length, b._bits.Length);
        
            for (int i = 0; i < minLength; i++) {
                result._bits[i] = a._bits[i] | b._bits[i];
            }
            
            BitMask larger = a._bits.Length > b._bits.Length ? a : b;
            for (int i = minLength; i < resultLength; i++) {
                result._bits[i] = larger._bits[i];
            }
        
            return result;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private static (int arrayIndex, int bitIndex) GetIndices(int id) {
            int arrayIndex = id / BitsPerElement;
            int bitIndex = id % BitsPerElement;
            return (arrayIndex, bitIndex);
        }
        
        public struct Enumerator : IDisposable {
            private uint[] _bits;
            private int _currentArrayIndex;
            private int _currentBitIndex;

            internal Enumerator (in BitMask data) {
                _bits = data._bits;
                _currentArrayIndex = 0;
                _currentBitIndex = -1;
            }

            public int Current {
                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                get => _currentArrayIndex * BitsPerElement + _currentBitIndex;
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public bool MoveNext () {
                for (int arrayIndex = _currentArrayIndex; arrayIndex < _bits.Length; arrayIndex++) {
                    uint chunk = _bits[arrayIndex];
                    if (chunk == 0) {
                        continue;
                    }
        
                    for (int bitIndex = _currentBitIndex + 1; bitIndex < BitsPerElement; bitIndex++) {
                        if ((chunk & (1u << bitIndex)) != 0) {
                            _currentArrayIndex = arrayIndex;
                            _currentBitIndex = bitIndex;
                            return true;
                        }
                    }

                    _currentBitIndex = -1;
                }

                return false;
            }

            public void Dispose() {
                _bits = null;
                _currentArrayIndex = 0;
                _currentBitIndex = 0;
            }
        }
    }   
}