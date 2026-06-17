using System.Collections.Generic;
using Pixagen.Ecs.Collections;

namespace Pixagen.Ecs.Runtime
{
    internal interface IComponentsStorage
    {
        IReadOnlyList<int> OneTickStorages { get; }
        ref SparseArray<IComponentStorage> Storages { get; }
        IComponentStorage Get(int componentId);
    }
}
