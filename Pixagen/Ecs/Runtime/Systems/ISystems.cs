using System;
using System.Collections.Generic;

namespace Pixagen.Ecs.Runtime
{
    public interface ISystems : IDisposable
    {
        event Action<SystemExecutionException> SystemException;

        IWorld World { get; }
        IReadOnlyList<ISystem> AllSystems { get; }
        ISystems Add(ISystem system);
        void Init();
        void Update();
        void Update(int fixedStepCount);

        void SetActiveGroup(string groupName, bool value);
        bool IsActiveGroup(string groupName);
    }
}
