using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class HierarchyDirtyQueueInitSystem : IInitSystem
{
    private readonly WorldInject _world = default;
    private readonly FilterInject<Include<HierarchyDirtyQueue>> _queues = default;
    private readonly ComponentInject<Info> _infos = default;

    public void Init()
    {
        if (_queues.Value.GetCount() > 0)
        {
            return;
        }

        Entity queue = _world.Create<HierarchyDirtyQueue>();
        _infos.Add(queue, Info.Create(nameof(HierarchyDirtyQueue)));
    }
}
