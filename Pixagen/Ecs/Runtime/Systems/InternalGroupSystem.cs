using System;
using System.Collections.Generic;

namespace Pixagen.Ecs.Runtime
{
    internal class InternalGroupSystem : IInitSystem, IPreUpdateSystem, IFixedUpdateSystem, IUpdateSystem, ILateUpdateSystem, IDisposable
    {
        private Systems _systems;
        private readonly SystemContainer _systemContainer;
        private bool _isActive;
        private bool _isDisposed;

        public bool IsActive => _isActive;

        public InternalGroupSystem(Systems systems, IGroupSystem group)
        {
            _systems = systems;
            _isActive = group.State;
            _systemContainer = new SystemContainer(group.Systems.Length, systems.HandleSystemException);

#if DEBUG
            _systemContainer.OnAddSystem += systems.RegisterSystem;
            _systemContainer.OnStartSystemExecute += systems.StartSystemExecute;
            _systemContainer.OnEndSystemExecute += systems.EndSystemExecute;
#endif

            for (int i = 0; i < group.Systems.Length; i++)
            {
                Add(group.Systems[i]);
            }
        }

        private void Add(ISystem system)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InternalGroupSystem));
            }

            if (system is IGroupSystem groupSystem)
            {
                Add(_systems.CreateGroupSystem(groupSystem));
                return;
            }

            _systemContainer.AddSystem(system);
        }

        public void Init()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InternalGroupSystem));
            }

            _systemContainer.Init();
        }

        public void FixedUpdate()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InternalGroupSystem));
            }

            if (!_isActive)
            {
                return;
            }

            _systemContainer.FixedUpdate(1);
        }

        public void PreUpdate()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InternalGroupSystem));
            }

            if (!_isActive)
            {
                return;
            }

            _systemContainer.PreUpdate();
        }

        public void SetActive(bool value)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InternalGroupSystem));
            }

            _isActive = value;
        }

        public void Update()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InternalGroupSystem));
            }

            if (!_isActive)
            {
                return;
            }

            _systemContainer.Update();
        }

        public void LateUpdate()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InternalGroupSystem));
            }

            if (!_isActive)
            {
                return;
            }

            _systemContainer.LateUpdate();
        }

        internal IReadOnlyList<ISystem> GetAllSystems()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InternalGroupSystem));
            }

            return _systemContainer.GetAllSystems();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _systems = null;
            _systemContainer.Dispose();
        }
    }
}
