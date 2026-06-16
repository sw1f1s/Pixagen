using System;
using System.Collections.Generic;
using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;
using Pixagen.Ecs.DI;

namespace Pixagen.Ecs.Runtime {
    public sealed class Systems : ISystems {
#if DEBUG
        public static readonly Dictionary<IWorld, Systems> SystemsMap = new Dictionary<IWorld, Systems>();
        public static event Action<IWorld, ISystem> OnAddSystem;
        public static event Action<IWorld, ISystem> OnStartSystemExecute;
        public static event Action<IWorld, ISystem> OnEndSystemExecute;
#endif
        private IWorld _world;
        private readonly SystemContainer _systemContainer;
        private readonly Dictionary<string, InternalGroupSystem> _groupSystems;
        private readonly List<object> _groupInjects;
        private Time _time;
        private bool _isDisposed;
        
        public IWorld World => _world;
        public IReadOnlyList<ISystem> AllSystems => _systemContainer.GetAllSystems();
        internal IReadOnlyList<object> GroupInjects => _groupInjects;
        
        public Systems(IWorld world) {
            _world = world;
            _systemContainer = new SystemContainer((int)world.Options.SystemsCapacity);
            _groupSystems = new Dictionary<string, InternalGroupSystem>((int)world.Options.SystemsCapacity);
            _groupInjects = new List<object>((int)world.Options.SystemsCapacity);
#if DEBUG
            SystemsMap[_world] = this;
            _systemContainer.OnAddSystem += RegisterSystem;
            _systemContainer.OnStartSystemExecute += StartSystemExecute;
            _systemContainer.OnEndSystemExecute += EndSystemExecute;
#endif
        }

        public ISystems Add(ISystem system) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(Systems));
            }

            if (system is IGroupSystem groupSystem) {
                return Add(CreateGroupSystem(groupSystem));
            }
            
            _systemContainer.AddSystem(system);
            return this;
        }

        public void Init() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(Systems));
            }

            Add(new RemoveOneTickComponentSystem(_world));
            _systemContainer.Init();
        }

        public void Update() {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(Systems));
            }
            _systemContainer.PreUpdate();
            _systemContainer.FixedUpdate(GetFixedStepCount());
            _systemContainer.Update();
            _systemContainer.LateUpdate();
        }

        internal void SetTime(Time time) {
            _time = time;
        }

#if DEBUG
        public void RegisterSystem(ISystem system) {
            OnAddSystem?.Invoke(_world, system);
        }
        public void StartSystemExecute(ISystem system) {
            OnStartSystemExecute?.Invoke(_world, system);
        }
        public void EndSystemExecute(ISystem system) {
            OnEndSystemExecute?.Invoke(_world, system);
        }
#endif

        public void Dispose() {
            if (_isDisposed) {
                return;
            }

#if DEBUG
            SystemsMap.Remove(_world);
            _systemContainer.OnAddSystem -= RegisterSystem;
            _systemContainer.OnStartSystemExecute -= StartSystemExecute;
            _systemContainer.OnEndSystemExecute -= EndSystemExecute;
#endif
            
            _isDisposed = true;
            _systemContainer.Dispose();
            DisposeInjects();
            _world = null;
            _time = null;
            _groupSystems.Clear();
            _groupInjects.Clear();
        }

        private int GetFixedStepCount() {
            return _time is not null
                ? _time.ConsumeFixedSteps()
                : 1;
        }

        private void DisposeInjects() {
            for (int i = _groupInjects.Count - 1; i >= 0; i--) {
                if (_groupInjects[i] is IDisposeInject disposeInject) {
                    disposeInject.DisposeInject();
                }
            }
        }
        
#region Groups
        internal InternalGroupSystem CreateGroupSystem(IGroupSystem system) {
            RegisterGroupInjects(system);
            var g = new InternalGroupSystem(this, system);
            _groupSystems.Add(system.GroupName, g);
            return g;
        }

        private void RegisterGroupInjects(IGroupSystem system) {
            object[] injects = system.Injects;
            for (int i = 0; i < injects.Length; i++) {
                if (injects[i] is not null) {
                    _groupInjects.Add(injects[i]);
                }
            }
        }

        public void SetActiveGroup(string groupName, bool value) {
            if (_groupSystems.TryGetValue(groupName, out var group)) {
                group.SetActive(value);
            }
        }

        public bool IsActiveGroup(string groupName) {
            if (_groupSystems.TryGetValue(groupName, out var group)) {
                return group.IsActive;
            }

            return false;
        }
#endregion
    }   
}
