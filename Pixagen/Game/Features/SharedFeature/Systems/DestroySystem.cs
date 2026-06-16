using Pixagen.Game.Features.PhysicsFeature.Runtime;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.SharedFeature.Systems;

public sealed class DestroySystem : IUpdateSystem
{
    private readonly List<Entity> _pending = new();
    private readonly HashSet<Entity> _destroyed = new();

    private readonly CustomInject<PhysicsWorld> _physicsWorld = default;
    private readonly CustomInject<RenderSceneCache> _renderSceneCache = default;
    private readonly FilterInject<Include<DestroyOneTick>> _entities = default;
    private readonly WorldInject _world = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<PhysicsBodyReference> _references = default;

    public void Update()
    {
        _pending.Clear();
        _destroyed.Clear();

        foreach (Entity entity in _entities.Value)
        {
            _pending.Add(entity);
        }

        if (_pending.Count == 0)
        {
            return;
        }

        foreach (Entity entity in _pending)
        {
            DestroyEntity(entity);
        }

        _renderSceneCache.Value.InvalidateStatic();
    }

    private void DestroyEntity(Entity entity)
    {
        if (!_entityState.Value.IsAlive(entity) || !_destroyed.Add(entity))
        {
            return;
        }

        if (_references.Has(entity))
        {
            ref PhysicsBodyReference reference = ref _references.Get(entity);
            _physicsWorld.Value.RemoveBody(reference);
            reference.Active = false;
        }

        _world.Destroy(entity);
    }
}
