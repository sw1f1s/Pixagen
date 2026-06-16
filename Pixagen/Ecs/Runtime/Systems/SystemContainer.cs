using System;
using System.Collections.Generic;

namespace Pixagen.Ecs.Runtime {
    internal class SystemContainer : IDisposable {
#if DEBUG
        public event Action<ISystem> OnAddSystem;
        public event Action<ISystem> OnStartSystemExecute;
        public event Action<ISystem> OnEndSystemExecute;
#endif
        
        private readonly List<ISystem> _allSystems;
        private readonly List<IInitSystem> _initSystems;
        private readonly List<IPreUpdateSystem> _preUpdateSystems;
        private readonly List<IFixedUpdateSystem> _fixedUpdateSystems;
        private readonly List<IUpdateSystem> _updateSystems;
        private readonly List<ILateUpdateSystem> _lateUpdateSystems;
        private readonly List<ISystem> _cachedSystems;
        private bool _isDisposed;

        internal SystemContainer(int capacity) {
            _allSystems = new List<ISystem>(capacity);
            _initSystems = new List<IInitSystem>(capacity);
            _preUpdateSystems = new List<IPreUpdateSystem>(capacity);
            _fixedUpdateSystems = new List<IFixedUpdateSystem>(capacity);
            _updateSystems = new List<IUpdateSystem>(capacity);
            _lateUpdateSystems = new List<ILateUpdateSystem>(capacity);
            _cachedSystems = new List<ISystem>(capacity);
        }

        internal void AddSystem(ISystem system) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }
            
            _allSystems.Add(system);
            
            if (system is IInitSystem initSystem) {
                _initSystems.Add(initSystem);
            }

            if (system is IPreUpdateSystem preUpdateSystem) {
                _preUpdateSystems.Add(preUpdateSystem);
            }

            if (system is IFixedUpdateSystem fixedUpdateSystem) {
                _fixedUpdateSystems.Add(fixedUpdateSystem);
            }
            
            if (system is IUpdateSystem updateSystem) {
                _updateSystems.Add(updateSystem);
            }

            if (system is ILateUpdateSystem lateUpdateSystem) {
                _lateUpdateSystems.Add(lateUpdateSystem);
            }
            
#if DEBUG
            if (system is not InternalGroupSystem) {
                OnAddSystem?.Invoke(system);   
            }
#endif
        }

        internal void Init() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }
            
            for (int i = 0; i < _initSystems.Count; i++) {
                _initSystems[i].Init();
            }
        }

        internal void PreUpdate() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }

            for (int i = 0; i < _preUpdateSystems.Count; i++) {
#if DEBUG
                if (_preUpdateSystems[i] is not InternalGroupSystem) {
                    OnStartSystemExecute?.Invoke(_preUpdateSystems[i]);
                }
#endif
                _preUpdateSystems[i].PreUpdate();
#if DEBUG
                if (_preUpdateSystems[i] is not InternalGroupSystem) {
                    OnEndSystemExecute?.Invoke(_preUpdateSystems[i]);
                }
#endif
            }
        }

        internal void FixedUpdate(int stepCount) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }

            for (int step = 0; step < stepCount; step++) {
                for (int i = 0; i < _fixedUpdateSystems.Count; i++) {
#if DEBUG
                    if (_fixedUpdateSystems[i] is not InternalGroupSystem) {
                        OnStartSystemExecute?.Invoke(_fixedUpdateSystems[i]);
                    }
#endif
                    _fixedUpdateSystems[i].FixedUpdate();
#if DEBUG
                    if (_fixedUpdateSystems[i] is not InternalGroupSystem) {
                        OnEndSystemExecute?.Invoke(_fixedUpdateSystems[i]);
                    }
#endif
                }
            }
        }

        internal void Update() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }

            for (int i = 0; i < _updateSystems.Count; i++) {
#if DEBUG
                if (_updateSystems[i] is not InternalGroupSystem) {
                    OnStartSystemExecute?.Invoke(_updateSystems[i]);
                }
#endif
                _updateSystems[i].Update();
#if DEBUG
                if (_updateSystems[i] is not InternalGroupSystem) {
                    OnEndSystemExecute?.Invoke(_updateSystems[i]);
                }
#endif
            }
        }

        internal void LateUpdate() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }

            for (int i = 0; i < _lateUpdateSystems.Count; i++) {
#if DEBUG
                if (_lateUpdateSystems[i] is not InternalGroupSystem) {
                    OnStartSystemExecute?.Invoke(_lateUpdateSystems[i]);
                }
#endif
                _lateUpdateSystems[i].LateUpdate();
#if DEBUG
                if (_lateUpdateSystems[i] is not InternalGroupSystem) {
                    OnEndSystemExecute?.Invoke(_lateUpdateSystems[i]);
                }
#endif
            }
        }

        internal IReadOnlyList<ISystem> GetAllSystems() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }
            
            _cachedSystems.Clear();
            foreach (var system in _allSystems) {
                if (system is InternalGroupSystem internalGroupSystem) {
                    _cachedSystems.AddRange(internalGroupSystem.GetAllSystems());
                } else {
                    _cachedSystems.Add(system);
                }
            }
            
            return _cachedSystems;
        }

        public void Dispose() {
            _isDisposed = true;
            _allSystems.Clear();
            _initSystems.Clear();
            _preUpdateSystems.Clear();
            _fixedUpdateSystems.Clear();
            _updateSystems.Clear();
            _lateUpdateSystems.Clear();
            _cachedSystems.Clear();
        }
    }   
}
