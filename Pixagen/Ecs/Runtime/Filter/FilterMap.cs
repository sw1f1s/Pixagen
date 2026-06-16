using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace Pixagen.Ecs.Runtime {
    internal class FilterMap : IDisposable {
        private readonly List<Filter> _filters;
        private readonly Dictionary<int, List<int>> _filterComponentsMaps;
        private readonly Dictionary<int, List<int>> _filterMaskMaps;
        private readonly List<(int ComponentId, int EntityId)> _pendingDirty;
        private readonly Options _options;
        private IWorld _world;

        public FilterMap(IWorld world, in Options options) {
            _world = world;
            _options = options;
            _filters = new List<Filter>((int)_options.FilterCapacity);
            _filterComponentsMaps = new Dictionary<int, List<int>>((int)_options.FilterCapacity);
            _filterMaskMaps = new Dictionary<int, List<int>>((int)_options.FilterCapacity);
            _pendingDirty = new List<(int, int)>((int)_options.EntityCapacity * (int)_options.ComponentCapacity);
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Filter GetFilter(FilterMask mask) {
            int hashId = mask.GetHashId();
            if (TryGetFilter(mask, hashId, out Filter filter)) {
                return filter;
            }

            var newFilter = CreateNewFilter(mask, hashId);
            foreach (var componentId in mask.GetIncludes()) {
                _filterComponentsMaps.TryAdd(componentId, new List<int>());
                _filterComponentsMaps[componentId].Add(_filters.Count - 1);
            }
            
            foreach (var componentId in mask.GetExcludes()) {
                _filterComponentsMaps.TryAdd(componentId, new List<int>());
                _filterComponentsMaps[componentId].Add(_filters.Count - 1);
            }
            return newFilter;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void AddDirtyEntity(int componentId, int entityId) {
            _pendingDirty.Add((componentId, entityId));
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void UpdateFiltersDirty() {
            if (_pendingDirty.Count == 0) {
                return;
            }
            
            for (int i = 0; i < _pendingDirty.Count; i++) {
                var (componentId, entityId) = _pendingDirty[i];
                if (_filterComponentsMaps.TryGetValue(componentId, out var filterIndexes)) {
                    for (int j = 0; j < filterIndexes.Count; j++) {
                        _filters[filterIndexes[j]].SetDirty(entityId);
                    }
                }
            }
            
            _pendingDirty.Clear();
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private bool TryGetFilter(FilterMask mask, int hashId, out Filter filter) {
            filter = null;
            if (_filterMaskMaps.TryGetValue(hashId, out var collisions)) {
                for (int i = 0; i < collisions.Count; i++) {
                    var f = _filters[collisions[i]];
                    if (mask.GetIncludes().HasAllCollision(f.Includes) && mask.GetExcludes().HasAllCollision(f.Excludes)) {
                        filter = f;
                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        private Filter CreateNewFilter(FilterMask mask, int hashId) {
            var newFilter = new Filter(mask, this, _world, in _options);
            int filterIndex = _filters.Count;
            _filters.Add(newFilter);
            if (_filterMaskMaps.TryGetValue(hashId, out var collisions)) {
                collisions.Add(filterIndex);
            }else {
                _filterMaskMaps.Add(hashId, new List<int>() { filterIndex }); 
            }

            return newFilter;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            foreach (var filter in _filters) {
                filter.Dispose();
            }
            _filters.Clear();
            _filterMaskMaps.Clear();
            _pendingDirty.Clear();
            _filterComponentsMaps.Clear();
        }

        public void Dispose() {
            _world = null;
            foreach (var filter in _filters) {
                filter.Dispose();
            }
            _filters.Clear();
            _filterMaskMaps.Clear();
            _filterComponentsMaps.Clear();
            _pendingDirty.Clear();
        }
    }   
}