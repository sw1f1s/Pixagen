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
        private readonly Action<Exception, ISystem, string> _exceptionHandler;
        private bool _isDisposed;

        internal SystemContainer(int capacity, Action<Exception, ISystem, string> exceptionHandler) {
            _allSystems = new List<ISystem>(capacity);
            _initSystems = new List<IInitSystem>(capacity);
            _preUpdateSystems = new List<IPreUpdateSystem>(capacity);
            _fixedUpdateSystems = new List<IFixedUpdateSystem>(capacity);
            _updateSystems = new List<IUpdateSystem>(capacity);
            _lateUpdateSystems = new List<ILateUpdateSystem>(capacity);
            _cachedSystems = new List<ISystem>(capacity);
            _exceptionHandler = exceptionHandler;
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
                IInitSystem system = _initSystems[i];
                ExecuteSystem(system, nameof(IInitSystem.Init), system.Init);
            }
        }

        internal void PreUpdate() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }

            for (int i = 0; i < _preUpdateSystems.Count; i++) {
                IPreUpdateSystem system = _preUpdateSystems[i];
                ExecuteSystem(system, nameof(IPreUpdateSystem.PreUpdate), system.PreUpdate);
            }
        }

        internal void FixedUpdate(int stepCount) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }

            for (int step = 0; step < stepCount; step++) {
                for (int i = 0; i < _fixedUpdateSystems.Count; i++) {
                    IFixedUpdateSystem system = _fixedUpdateSystems[i];
                    ExecuteSystem(system, nameof(IFixedUpdateSystem.FixedUpdate), system.FixedUpdate);
                }
            }
        }

        internal void Update() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }

            for (int i = 0; i < _updateSystems.Count; i++) {
                IUpdateSystem system = _updateSystems[i];
                ExecuteSystem(system, nameof(IUpdateSystem.Update), system.Update);
            }
        }

        internal void LateUpdate() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(SystemContainer));
            }

            for (int i = 0; i < _lateUpdateSystems.Count; i++) {
                ILateUpdateSystem system = _lateUpdateSystems[i];
                ExecuteSystem(system, nameof(ILateUpdateSystem.LateUpdate), system.LateUpdate);
            }
        }

        private void ExecuteSystem(ISystem system, string stage, Action execute) {
            try {
#if DEBUG
                if (system is not InternalGroupSystem) {
                    OnStartSystemExecute?.Invoke(system);
                }
#endif
                execute();
            } catch (Exception exception) {
                HandleExecutionException(exception, system, stage);
            } finally {
#if DEBUG
                if (system is not InternalGroupSystem) {
                    OnEndSystemExecute?.Invoke(system);
                }
#endif
            }
        }

        private void HandleExecutionException(Exception exception, ISystem system, string stage) {
            try {
                _exceptionHandler(exception, system, stage);
            } catch (Exception handlerException) {
                Console.Error.WriteLine(
                    $"System exception handler failed for {system.GetType().FullName}.{stage}.{Environment.NewLine}" +
                    $"{handlerException}{Environment.NewLine}" +
                    $"Original exception:{Environment.NewLine}{exception}");
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
