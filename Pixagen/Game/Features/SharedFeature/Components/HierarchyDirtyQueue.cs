using Pixagen.Ecs.Collections;

namespace Pixagen.Game.Features.SharedFeature.Components;

public struct HierarchyDirtyQueue : IComponent, IAutoPoolComponent<HierarchyDirtyQueue>
{
    public PooledList<Entity> TransformDirtyEntities;

    public void Reset(ref HierarchyDirtyQueue c, IPoolFactory poolFactory)
    {
        c.TransformDirtyEntities?.Return();
        c.TransformDirtyEntities = poolFactory.Rent<Entity>();
    }

    public void Destroy(ref HierarchyDirtyQueue c, IPoolFactory poolFactory)
    {
        c.TransformDirtyEntities?.Return();
        c.TransformDirtyEntities = null;
    }
}
