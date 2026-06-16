using System.Text.Json.Serialization;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.Collections;

namespace Pixagen.Game.Features.SharedFeature.Components;

public struct Children : IComponent, IAutoPoolComponent<Children>, IAutoCopyComponent<Children>
{
    [JsonIgnore]
    public PooledList<Entity> Entities;

    public void Reset(ref Children c, IPoolFactory poolFactory)
    {
        c.Entities?.Return();
        c.Entities = poolFactory.Rent<Entity>();
    }

    public void Destroy(ref Children c, IPoolFactory poolFactory)
    {
        c.Entities?.Return();
        c.Entities = null;
    }

    public void Copy(ref Children src, ref Children dst)
    {
        dst.Entities = src.Entities.Copy();
    }
}
