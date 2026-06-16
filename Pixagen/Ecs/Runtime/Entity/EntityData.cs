using System;
using System.Runtime.CompilerServices;
namespace Pixagen.Ecs.Runtime {
    internal struct EntityData : IDisposable {
        private Entity _entity;
        private int _componentCount;
        private bool _isDisposed;
        
        public int Id => _entity.Id;
        public int ComponentCount => _componentCount;
        public bool IsEmpty => _componentCount == 0;
        
        public EntityData(Entity entity) {
            _entity = entity;
            _componentCount = 0;
            _isDisposed = false;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public readonly Entity GetEntity() => _entity;

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void AddComponent() {
            _componentCount++;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent() {
            if (_componentCount > 0) {
                _componentCount--;
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void ClearComponents() {
            _componentCount = 0;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            _entity = _entity.ResetGen();
            _componentCount = 0;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void IncreaseGen() => 
            _entity = _entity.IncreaseGen();
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void IncreaseGen(int gen) => 
            _entity = new Entity(_entity.Id, gen, _entity.WorldId);

        public void Dispose() {
            if (_isDisposed) {
                return;
            }
            
            _isDisposed = true;
            _entity = default;
            _componentCount = 0;
        }
    } 
}
